using System.Reflection;
using Libreria.Application.Models;
using Libreria.Application.Services;
using Libreria.Application.Validators;
using Libreria.Client.Pages.Cuenta;
using Libreria.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Application.Tests;

public class CambioContrasenaTests
{
    [Fact]
    public async Task CambiarContrasena_Valida_UsaIdentidadActualYHashSeguro()
    {
        var hasher = new ContrasenaHasher();
        var repo = new UsuarioListadoRepositoryFake { HashActivo = hasher.Hashear("Actual123!") };
        var servicio = CrearServicio(repo);

        var resultado = await servicio.CambiarContrasenaAsync(Input());

        Assert.True(resultado.Exitoso);
        Assert.Equal(17, repo.UsuarioContrasenaId);
        Assert.Equal(1, repo.CambiosContrasena);
        Assert.NotEqual("NuevaClave2!", repo.NuevoHash);
        Assert.True(hasher.Verificar(repo.NuevoHash!, "NuevaClave2!"));
    }

    [Theory]
    [InlineData("Incorrecta1!", "NuevaClave2!", "NuevaClave2!", "Input.ContrasenaActual", "La contraseña actual es incorrecta.")]
    [InlineData("Actual123!", "NuevaClave2!", "OtraClave3!", "Input.Confirmacion", "La confirmación no coincide con la nueva contraseña.")]
    [InlineData("Actual123!", "Actual123!", "Actual123!", "Input.NuevaContrasena", "La nueva contraseña debe ser distinta de la actual.")]
    [InlineData("", "NuevaClave2!", "NuevaClave2!", "Input.ContrasenaActual", "Ingrese su contraseña actual.")]
    public async Task CambiarContrasena_DatosInvalidos_NoActualiza(
        string actual, string nueva, string confirmacion, string campo, string mensaje)
    {
        var repo = new UsuarioListadoRepositoryFake { HashActivo = new ContrasenaHasher().Hashear("Actual123!") };

        var resultado = await CrearServicio(repo).CambiarContrasenaAsync(new CambioContrasenaInput
            { ContrasenaActual = actual, NuevaContrasena = nueva, Confirmacion = confirmacion });

        Assert.False(resultado.Exitoso);
        Assert.Equal(mensaje, resultado.Errores[campo]);
        Assert.Equal(0, repo.CambiosContrasena);
    }

    [Fact]
    public async Task CambiarContrasena_ReutilizaTodosLosMensajesDeUS69()
    {
        var repo = new UsuarioListadoRepositoryFake();
        var input = Input();
        input.NuevaContrasena = input.Confirmacion = "abc";

        var resultado = await CrearServicio(repo).CambiarContrasenaAsync(input);

        foreach (var mensaje in new PasswordPolicyValidator().Validar("abc"))
            Assert.Contains(mensaje, resultado.Errores["Input.NuevaContrasena"]);
        Assert.Equal(0, repo.CambiosContrasena);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task CambiarContrasena_SinSesion_NoConsultaNiActualiza(int? usuarioId)
    {
        var repo = new UsuarioListadoRepositoryFake();

        Assert.False((await CrearServicio(repo, usuarioId).CambiarContrasenaAsync(Input())).Exitoso);
        Assert.Null(repo.UsuarioContrasenaId);
        Assert.Equal(0, repo.CambiosContrasena);
    }

    [Fact]
    public async Task CambiarContrasena_UsuarioSinAccesoActivo_NoActualiza()
    {
        var repo = new UsuarioListadoRepositoryFake();
        Assert.False((await CrearServicio(repo).CambiarContrasenaAsync(Input())).Exitoso);
        Assert.Equal(0, repo.CambiosContrasena);
    }

    [Fact]
    public async Task CambiarContrasena_NoPermiteSuperarLimiteDelLoginExistente()
    {
        var repo = new UsuarioListadoRepositoryFake();
        var input = Input();
        input.NuevaContrasena = input.Confirmacion = "Aa1!" + new string('a', 125);

        var resultado = await CrearServicio(repo).CambiarContrasenaAsync(input);

        Assert.Equal("La contraseña no puede superar los 128 caracteres.", resultado.Errores["Input.NuevaContrasena"]);
        Assert.Equal(0, repo.CambiosContrasena);
    }

    [Fact]
    public async Task Pagina_ExigeSesionYSoloRecibeLosTresCamposDeContrasena()
    {
        Assert.NotNull(typeof(CambiarContrasenaModel).GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal(new[] { "Confirmacion", "ContrasenaActual", "NuevaContrasena" },
            typeof(CambioContrasenaInput).GetProperties().Select(p => p.Name).OrderBy(n => n));
        var repo = new UsuarioListadoRepositoryFake();
        var pagina = new CambiarContrasenaModel(CrearServicio(repo))
            { PageContext = new PageContext { HttpContext = new DefaultHttpContext() } };

        Assert.IsType<ChallengeResult>(await pagina.OnPostAsync());
        Assert.Equal(0, repo.CambiosContrasena);
    }

    private static CambioContrasenaInput Input() => new()
        { ContrasenaActual = "Actual123!", NuevaContrasena = "NuevaClave2!", Confirmacion = "NuevaClave2!" };

    private static UsuarioService CrearServicio(UsuarioListadoRepositoryFake repo, int? usuarioId = 17) =>
        // Los puertos de alta no intervienen en este caso de uso.
        new(repo, new GeneradorNombreUsuario(repo), null!, null!, new ConexionBajaFake(),
            new UsuarioActualFake(usuarioId), new ContrasenaHasher(), new PasswordPolicyValidator());
}
