using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Libreria.Client.Adapters.Usuarios;
using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Pages.Cuenta;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Xunit;

namespace Libreria.Client.Tests.Pages;

/// <summary>US-129: política validada en el Front y cierre de sesión al cambiar la contraseña.</summary>
public sealed class CambiarContrasenaModelTests
{
    [Theory]
    [InlineData("corta1!A")]
    [InlineData("Nueva#2026x")]
    public void PoliticaValida_NoTieneErrores(string nueva)
    {
        Assert.Empty(Validar(new CambioContrasenaInput { ContrasenaActual = "Actual#2026", NuevaContrasena = nueva, Confirmacion = nueva }));
    }

    [Theory]
    [InlineData("Corta1!")]
    [InlineData("sinmayuscula1!")]
    [InlineData("SINMINUSCULA1!")]
    [InlineData("SinNumero!!")]
    [InlineData("SinEspecial12")]
    public void PoliticaIncumplida_SeRechazaEnElFront(string nueva)
    {
        var errores = Validar(new CambioContrasenaInput { ContrasenaActual = "Actual#2026", NuevaContrasena = nueva, Confirmacion = nueva });

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(CambioContrasenaInput.NuevaContrasena)));
    }

    [Fact]
    public void ConfirmacionDistinta_SeRechazaEnElFront()
    {
        var errores = Validar(new CambioContrasenaInput { ContrasenaActual = "Actual#2026", NuevaContrasena = "Nueva#2026x", Confirmacion = "Nueva#2026y" });

        Assert.Contains(errores, e => e.ErrorMessage == "La confirmación no coincide con la nueva contraseña.");
    }

    [Fact]
    public async Task CambioExitoso_CierraLaSesionEnElFrontYEnKeycloak()
    {
        var api = new UsuariosApiFalsa();
        var pagina = Crear(api);

        var resultado = await pagina.OnPostAsync();

        var salida = Assert.IsType<SignOutResult>(resultado);
        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, salida.AuthenticationSchemes);
        Assert.Contains(OpenIdConnectDefaults.AuthenticationScheme, salida.AuthenticationSchemes);
        Assert.Equal(MotivosSesion.UrlLogin(MotivosSesion.ContrasenaCambiada), salida.Properties!.RedirectUri);
        Assert.True(api.CambioSolicitado);
    }

    [Fact]
    public async Task CambioRechazado_MuestraElMensajeYNoCierraLaSesion()
    {
        var api = new UsuariosApiFalsa { Error = "La contraseña actual es incorrecta." };
        var pagina = Crear(api);

        var resultado = await pagina.OnPostAsync();

        Assert.IsType<PageResult>(resultado);
        Assert.Contains("La contraseña actual es incorrecta.",
            pagina.ModelState[string.Empty]!.Errors.Select(e => e.ErrorMessage));
    }

    private static List<ValidationResult> Validar(CambioContrasenaInput input)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), errores, validateAllProperties: true);
        return errores;
    }

    private static CambiarContrasenaModel Crear(IUsuariosApi api)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString())], "Prueba"))
        };
        return new CambiarContrasenaModel(api)
        {
            PageContext = new PageContext(new ActionContext(http, new RouteData(), new PageActionDescriptor())),
            Input = new CambioContrasenaInput { ContrasenaActual = "Actual#2026", NuevaContrasena = "Nueva#2026x", Confirmacion = "Nueva#2026x" }
        };
    }

    private sealed class UsuariosApiFalsa : IUsuariosApi
    {
        public string? Error { get; init; }
        public bool CambioSolicitado { get; private set; }

        public Task CambiarContrasenaAsync(ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
        {
            if (Error is not null) throw new ApiClientException("usuarios", System.Net.HttpStatusCode.BadRequest, Error);
            CambioSolicitado = true;
            return Task.CompletedTask;
        }

        public Task<bool> SesionActivaAsync(Guid keycloakId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> SesionActivaAsync(int usuarioId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<int, string>> ObtenerNombresAsync(IEnumerable<int> usuarioIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, string>> ObtenerNombresAsync(IEnumerable<Guid> usuarioIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<UserListItemDto>> ObtenerUsuariosAsync(string? estado = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> ObtenerRolesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<CreateUserResultDto?> CrearUsuarioAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DarDeBajaUsuarioAsync(Guid publicId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
