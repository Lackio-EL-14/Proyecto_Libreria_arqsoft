using System.Net;
using System.Security.Claims;
using Libreria.Client.Adapters.Usuarios;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Pages.Cuenta;
using Libreria.Client.Seguridad;
using Libreria.Client.Tests.Clients;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Tests;

public sealed class CambiarContrasenaModelTests
{
    private static CambiarContrasenaModel Crear(ManejadorHttpFalso handler, bool autenticado = true)
    {
        var context = new DefaultHttpContext();
        if (autenticado)
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "Test"));
        return new CambiarContrasenaModel(new UsuariosApiAdapter(ManejadorHttpFalso.CrearFabrica(handler)))
        {
            PageContext = new PageContext { HttpContext = context },
            Input = new CambioContrasenaInput
            {
                ContrasenaActual = "Actual#2026", NuevaContrasena = "Nueva#2026x", Confirmacion = "Nueva#2026x"
            }
        };
    }

    [Fact]
    public async Task CambioExitoso_CierraCookieYKeycloak_YVuelveAlLoginConMensaje()
    {
        var handler = new ManejadorHttpFalso(HttpStatusCode.NoContent);
        var result = Assert.IsType<SignOutResult>(await Crear(handler).OnPostAsync());
        Assert.Equal(new[] { CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme }, result.AuthenticationSchemes);
        Assert.Equal(MotivosSesion.UrlLogin(MotivosSesion.ContrasenaActualizada), result.Properties!.RedirectUri);
        Assert.Contains("nueva contraseña", MotivosSesion.Mensaje(MotivosSesion.ContrasenaActualizada));
        Assert.Equal(HttpMethod.Put, handler.UltimaPeticion!.Method);
    }

    [Fact]
    public async Task ContrasenaIncorrecta_MuestraErrorYLimpiaCampos_SinCerrarSesion()
    {
        var model = Crear(new ManejadorHttpFalso(HttpStatusCode.BadRequest,
            """{"status":400,"detail":"La contraseña actual es incorrecta."}"""));
        Assert.IsType<PageResult>(await model.OnPostAsync());
        Assert.Equal("La contraseña actual es incorrecta.", Assert.Single(model.ModelState[string.Empty]!.Errors).ErrorMessage);
        Assert.Empty(model.Input.ContrasenaActual);
        Assert.Empty(model.Input.NuevaContrasena);
        Assert.Empty(model.Input.Confirmacion);
    }

    [Fact]
    public async Task SinSesion_NoEnviaCambio()
    {
        var handler = new ManejadorHttpFalso(HttpStatusCode.NoContent);
        Assert.IsType<ChallengeResult>(await Crear(handler, false).OnPostAsync());
        Assert.Null(handler.UltimaPeticion);
    }
}
