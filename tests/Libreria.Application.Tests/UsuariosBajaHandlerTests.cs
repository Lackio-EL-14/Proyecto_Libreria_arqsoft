using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Services;
using Libreria.Client.Pages.Usuarios;
using Libreria.Domain;
using Libreria.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Application.Tests;

public class UsuariosBajaHandlerTests
{
    [Fact]
    public async Task Post_AdministradorConIdProtegido_DaDeBaja()
    {
        using var services = UsuariosIndexTests.CrearServicios();
        var seguridad = new UsuarioSeguridadServiceFake();
        var protector = new UrlProtector(new EphemeralDataProtectionProvider());
        var pagina = CrearPagina(services, seguridad, protector, RolesSistema.Administrador);

        Assert.IsType<RedirectToPageResult>(await pagina.OnPostDarDeBajaAsync(protector.Cifrar(Guid.NewGuid())));
        Assert.Equal(1, seguridad.Bajas);
        Assert.Equal("Usuario dado de baja correctamente.", pagina.TempData["MensajeExito"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("manipulado")]
    public async Task Post_IdNoProtegidoOInvalido_NoEjecutaBaja(string? id)
    {
        using var services = UsuariosIndexTests.CrearServicios();
        var seguridad = new UsuarioSeguridadServiceFake();
        var pagina = CrearPagina(services, seguridad,
            new UrlProtector(new EphemeralDataProtectionProvider()), RolesSistema.Administrador);

        Assert.IsType<BadRequestResult>(await pagina.OnPostDarDeBajaAsync(id));
        Assert.Equal(0, seguridad.Bajas);
    }

    [Fact]
    public async Task Post_Vendedor_DenegadoSinEjecutarBaja()
    {
        using var services = UsuariosIndexTests.CrearServicios();
        var seguridad = new UsuarioSeguridadServiceFake();
        var pagina = CrearPagina(services, seguridad,
            new UrlProtector(new EphemeralDataProtectionProvider()), RolesSistema.Vendedor);
        Assert.IsType<ForbidResult>(await pagina.OnPostDarDeBajaAsync("1"));
        Assert.Equal(0, seguridad.Bajas);
    }

    [Fact]
    public async Task Post_ReglaDeBajaRechazada_MuestraError()
    {
        using var services = UsuariosIndexTests.CrearServicios();
        var seguridad = new UsuarioSeguridadServiceFake
        {
            Resultado = ResultadoOperacion.Invalido(new Dictionary<string, string>
                { [string.Empty] = "No puede darse de baja a sí mismo." })
        };
        var protector = new UrlProtector(new EphemeralDataProtectionProvider());
        var pagina = CrearPagina(services, seguridad, protector, RolesSistema.Administrador);
        Assert.IsType<RedirectToPageResult>(await pagina.OnPostDarDeBajaAsync(protector.Cifrar(Guid.NewGuid())));
        Assert.Equal("No puede darse de baja a sí mismo.", pagina.TempData["MensajeError"]);
    }

    private static IndexModel CrearPagina(ServiceProvider services, UsuarioSeguridadServiceFake seguridad,
        IUrlProtector protector, string rol)
    {
        var http = new DefaultHttpContext { User = UsuariosIndexTests.CrearPrincipal(rol) };
        return new IndexModel(new ConsultaUsuarioService(new UsuarioListadoRepositoryFake()),
            services.GetRequiredService<IAuthorizationService>(), seguridad, protector)
        {
            PageContext = new PageContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new TempDataProviderFake())
        };
    }

    private sealed class TempDataProviderFake : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }
}
