using System.Reflection;
using System.Security.Claims;
using Libreria.Application.Services;
using Libreria.Client.Pages.Usuarios;
using Libreria.Client.Seguridad;
using Libreria.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Libreria.Infrastructure.Security;

namespace Libreria.Application.Tests;

public class UsuariosIndexTests
{
    [Fact]
    public void Pagina_ExigePoliticaAdministradorEnElServidor()
    {
        var permiso = typeof(IndexModel).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(permiso);
        Assert.Equal(PoliticasAcceso.SoloAdministrador, permiso.Policy);
        Assert.Null(typeof(IndexModel).GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Fact]
    public async Task OnGetAsync_Administrador_ObtieneListado()
    {
        using var services = CrearServicios();
        var esperado = ConsultaUsuarioServiceTests.CrearUsuario(EstadosUsuario.Activo);
        var repository = new UsuarioListadoRepositoryFake(esperado);
        var pagina = CrearPagina(services, repository, CrearPrincipal(RolesSistema.Administrador));

        Assert.IsType<PageResult>(await pagina.OnGetAsync());
        Assert.Equal(esperado, Assert.Single(pagina.Usuarios));
        Assert.Equal(1, repository.Consultas);
    }

    [Theory]
    [InlineData(RolesSistema.Vendedor)]
    [InlineData("OtroRol")]
    [InlineData(null)]
    public async Task OnGetAsync_AutenticadoSinPermiso_DenegadoSinConsultarDatos(string? rol)
    {
        using var services = CrearServicios();
        var repository = new UsuarioListadoRepositoryFake();
        var pagina = CrearPagina(services, repository, CrearPrincipal(rol));

        Assert.IsType<ForbidResult>(await pagina.OnGetAsync());
        Assert.Empty(pagina.Usuarios);
        Assert.Equal(0, repository.Consultas);
    }

    [Fact]
    public async Task OnGetAsync_Anonimo_ExigeLoginSinConsultarDatos()
    {
        using var services = CrearServicios();
        var repository = new UsuarioListadoRepositoryFake();
        var pagina = CrearPagina(services, repository, new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.IsType<ChallengeResult>(await pagina.OnGetAsync());
        Assert.Equal(0, repository.Consultas);
    }

    [Fact]
    public async Task OnGetAsync_ClaimAdministradorSinAutenticacion_ExigeLogin()
    {
        using var services = CrearServicios();
        var repository = new UsuarioListadoRepositoryFake();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, RolesSistema.Administrador) }));
        var pagina = CrearPagina(services, repository, principal);

        Assert.IsType<ChallengeResult>(await pagina.OnGetAsync());
        Assert.Equal(0, repository.Consultas);
    }

    [Fact]
    public async Task OnGetAsync_FiltroValido_DevuelveSoloElEstadoSeleccionado()
    {
        using var services = CrearServicios();
        var repository = new UsuarioListadoRepositoryFake(
            ConsultaUsuarioServiceTests.CrearUsuario(EstadosUsuario.Activo),
            ConsultaUsuarioServiceTests.CrearUsuario(EstadosUsuario.Pendiente));
        var pagina = CrearPagina(services, repository, CrearPrincipal(RolesSistema.Administrador));
        pagina.Estado = EstadosUsuario.Pendiente;

        Assert.IsType<PageResult>(await pagina.OnGetAsync());
        Assert.Equal(EstadosUsuario.Pendiente, Assert.Single(pagina.Usuarios).Estado);
    }

    [Fact]
    public async Task OnGetAsync_FiltroInvalido_MuestraErrorSinConsultarDatos()
    {
        using var services = CrearServicios();
        var repository = new UsuarioListadoRepositoryFake();
        var pagina = CrearPagina(services, repository, CrearPrincipal(RolesSistema.Administrador));
        pagina.Estado = "Desconocido";

        Assert.IsType<PageResult>(await pagina.OnGetAsync());
        Assert.False(pagina.ModelState.IsValid);
        Assert.StartsWith("Seleccione un estado de usuario válido.",
            Assert.Single(pagina.ModelState[nameof(pagina.Estado)]!.Errors).ErrorMessage);
        Assert.Equal(0, repository.Consultas);
    }

    [Fact]
    public async Task OnGetAsync_ListaVacia_DevuelvePaginaSinErrores()
    {
        using var services = CrearServicios();
        var pagina = CrearPagina(services, new UsuarioListadoRepositoryFake(),
            CrearPrincipal(RolesSistema.Administrador));

        Assert.IsType<PageResult>(await pagina.OnGetAsync());
        Assert.Empty(pagina.Usuarios);
        Assert.True(pagina.ModelState.IsValid);
    }

    internal static ServiceProvider CrearServicios()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddPolicy(
            PoliticasAcceso.SoloAdministrador, politica => politica.RequireRole(RolesSistema.Administrador)));
        return services.BuildServiceProvider();
    }

    internal static ClaimsPrincipal CrearPrincipal(string? rol)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "usuario-prueba") };
        if (rol is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Pruebas"));
    }

    private static IndexModel CrearPagina(ServiceProvider services,
        UsuarioListadoRepositoryFake repository, ClaimsPrincipal principal) => new(
            new ConsultaUsuarioService(repository), services.GetRequiredService<IAuthorizationService>(),
            new UsuarioSeguridadServiceFake(), new UrlProtector(new EphemeralDataProtectionProvider()))
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
}
