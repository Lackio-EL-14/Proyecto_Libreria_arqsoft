using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
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
/// <para>Desde US-126 cada microservicio toma al usuario del token validado: no se envían
/// cabeceras <c>X-Usuario-*</c>.</para>
/// </remarks>
public sealed class BearerTokenDelegatingHandler : DelegatingHandler
{
    /// <summary>
    /// Claves de <c>HttpContext.Items</c> que <c>ValidarEstadoUsuarioCookieEvents</c> llena mientras
    /// valida la cookie: en ese momento no se puede volver a autenticarla para leer el token.
    /// </summary>
    public const string ClaveTokenEnCurso = "Libreria.AccessToken";
    public const string ClaveVencimientoEnCurso = "Libreria.AccessTokenExpiresAt";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _tiempo;

    public BearerTokenDelegatingHandler(IHttpContextAccessor httpContextAccessor, TimeProvider tiempo)
    {
        _httpContextAccessor = httpContextAccessor;
        _tiempo = tiempo;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var contexto = _httpContextAccessor.HttpContext;
        var (token, venceEn) = contexto is null ? (null, null) : await LeerTokenAsync(contexto);

        if (token is not null)
        {
            if (venceEn is { } vence && vence <= _tiempo.GetUtcNow())
            {
                throw new SesionExpiradaException("El token de acceso venció.");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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
}
