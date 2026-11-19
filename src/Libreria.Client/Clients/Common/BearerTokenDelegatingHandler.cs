using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Libreria.Client.Clients.Common;

/// <summary>
/// Agrega <c>Authorization: Bearer &lt;token de Keycloak&gt;</c> a todas las llamadas de los
/// adaptadores hacia el gateway (US-125, contrato 6.3). Reemplaza a <c>UsuarioActualDelegatingHandler</c>.
/// </summary>
/// <remarks>
/// <para>Si el token ya venció, o si un servicio responde 401, lanza
/// <see cref="SesionExpiradaException"/>: <see cref="SesionExpiradaFilter"/> cierra la sesión
/// y lleva al login con un mensaje.</para>
/// <para><b>TRANSITORIO hasta US-126/127:</b> el MS Usuarios todavía identifica al usuario
/// actual por cabecera, así que solo hacia <c>/api/usuarios</c> se envían además
/// <c>X-Usuario-Id</c> (= <c>sub</c>) y <c>X-Usuario-Nombre</c> (= <c>preferred_username</c>).</para>
/// </remarks>
public sealed class BearerTokenDelegatingHandler : DelegatingHandler
{
    /// <summary>
    /// Claves de <c>HttpContext.Items</c> que <c>ValidarEstadoUsuarioCookieEvents</c> llena mientras
    /// valida la cookie: en ese momento no se puede volver a autenticarla para leer el token.
    /// </summary>
    public const string ClaveTokenEnCurso = "Libreria.AccessToken";
    public const string ClaveVencimientoEnCurso = "Libreria.AccessTokenExpiresAt";
    public const string ClavePrincipalEnCurso = "Libreria.PrincipalEnCurso";

    public const string UsuarioIdHeader = "X-Usuario-Id";
    public const string UsuarioNombreHeader = "X-Usuario-Nombre";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _tiempo;

    public BearerTokenDelegatingHandler(IHttpContextAccessor httpContextAccessor, TimeProvider tiempo)
    {
        _httpContextAccessor = httpContextAccessor;
        _tiempo = tiempo;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(UsuarioIdHeader);
        request.Headers.Remove(UsuarioNombreHeader);

        var contexto = _httpContextAccessor.HttpContext;
        var (token, venceEn) = contexto is null ? (null, null) : await LeerTokenAsync(contexto);

        if (token is not null)
        {
            if (venceEn is { } vence && vence <= _tiempo.GetUtcNow())
            {
                throw new SesionExpiradaException("El token de acceso venció.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            AgregarCabecerasTransitorias(request, contexto!);
        }

        var respuesta = await base.SendAsync(request, cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized && token is not null)
        {
            respuesta.Dispose();
            throw new SesionExpiradaException(
                $"El servicio respondió 401 a {request.Method} {request.RequestUri?.AbsolutePath}.");
        }

        return respuesta;
    }

    private static async Task<(string? Token, DateTimeOffset? VenceEn)> LeerTokenAsync(HttpContext contexto)
    {
        string? token, vencimiento;

        if (contexto.Items.TryGetValue(ClaveTokenEnCurso, out var enCurso) && enCurso is string tokenEnCurso)
        {
            token = tokenEnCurso;
            vencimiento = contexto.Items[ClaveVencimientoEnCurso] as string;
        }
        else if (contexto.User.Identity?.IsAuthenticated == true)
        {
            token = await contexto.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "access_token");
            vencimiento = await contexto.GetTokenAsync(CookieAuthenticationDefaults.AuthenticationScheme, "expires_at");
        }
        else
        {
            return (null, null);
        }

        if (string.IsNullOrEmpty(token))
        {
            return (null, null);
        }

        return (token, DateTimeOffset.TryParse(vencimiento, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var vence)
            ? vence
            : null);
    }

    private static void AgregarCabecerasTransitorias(HttpRequestMessage request, HttpContext contexto)
    {
        var ruta = request.RequestUri?.AbsolutePath;
        var esUsuarios =
            string.Equals(ruta, "/api/usuarios", StringComparison.OrdinalIgnoreCase) ||
            ruta?.StartsWith("/api/usuarios/", StringComparison.OrdinalIgnoreCase) == true;

        if (!esUsuarios)
        {
            return;
        }

        var principal = contexto.Items.TryGetValue(ClavePrincipalEnCurso, out var enCurso) &&
                        enCurso is ClaimsPrincipal principalEnCurso
            ? principalEnCurso
            : contexto.User;

        if (principal.ObtenerSub() is { } sub)
        {
            request.Headers.TryAddWithoutValidation(UsuarioIdHeader, sub.ToString("D"));
        }

        var nombreUsuario = principal.FindFirst(ClaimsKeycloak.NombreUsuario)?.Value;
        if (!string.IsNullOrWhiteSpace(nombreUsuario))
        {
            request.Headers.TryAddWithoutValidation(
                UsuarioNombreHeader,
                nombreUsuario.Replace("\r", string.Empty).Replace("\n", string.Empty));
        }
    }
}
