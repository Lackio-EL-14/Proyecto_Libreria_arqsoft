using System.Globalization;
using Libreria.Client.Clients.Common;
using Libreria.Client.Adapters.Usuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Libreria.Client.Seguridad;

/// <summary>
/// En cada petición valida la cookie de sesión (US-125):
/// <list type="bullet">
/// <item>el token de Keycloak guardado en la sesión no venció;</item>
/// <item>el usuario (por su <c>sub</c>) sigue activo en el MS Usuarios. Se guarda en caché unos
/// segundos, así una baja (US-128) corta la sesión casi enseguida, sin esperar a que venza el token.</item>
/// </list>
/// Si la rechaza, deja el motivo en <c>HttpContext.Items</c>: el desafío de OIDC lo convierte
/// en una redirección a <c>/Cuenta/Login?motivo=…</c> en lugar de volver a Keycloak (sin bucles).
/// </summary>
public class ValidarEstadoUsuarioCookieEvents : CookieAuthenticationEvents
{
    private const string CacheKeyPrefix = "sesion-activa:";

    private readonly IUsuariosApi _usuariosClient;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _tiempo;
    private readonly ILogger<ValidarEstadoUsuarioCookieEvents> _logger;
    private readonly TimeSpan _cacheDuration;

    public ValidarEstadoUsuarioCookieEvents(
        IUsuariosApi usuariosClient,
        IMemoryCache cache,
        IOptions<SesionActivaOptions> options,
        TimeProvider tiempo,
        ILogger<ValidarEstadoUsuarioCookieEvents> logger)
    {
        _usuariosClient = usuariosClient;
        _cache = cache;
        _tiempo = tiempo;
        _logger = logger;
        _cacheDuration = TimeSpan.FromSeconds(
            Math.Clamp(options.Value.ValidacionEstadoSegundos, 5, 60));
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var sub = context.Principal.ObtenerSub();
        var token = context.Properties.GetTokenValue("access_token");
        var vencimiento = context.Properties.GetTokenValue("expires_at");

        if (sub is null || string.IsNullOrEmpty(token))
        {
            await RechazarSesionAsync(context, MotivosSesion.Vencida);
            return;
        }

        if (DateTimeOffset.TryParse(vencimiento, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var vence) &&
            vence <= _tiempo.GetUtcNow())
        {
            await RechazarSesionAsync(context, MotivosSesion.Vencida);
            return;
        }

        var cacheKey = $"{CacheKeyPrefix}{sub:D}";

        try
        {
            if (!_cache.TryGetValue(cacheKey, out bool sesionActiva))
            {
                // La cookie se está validando: el handler del Bearer toma el token de aquí.
                var items = context.HttpContext.Items;
                items[BearerTokenDelegatingHandler.ClaveTokenEnCurso] = token;
                items[BearerTokenDelegatingHandler.ClaveVencimientoEnCurso] = vencimiento;
                try
                {
                    sesionActiva = await _usuariosClient.SesionActivaAsync(
                        sub.Value,
                        context.HttpContext.RequestAborted);
                }
                finally
                {
                    items.Remove(BearerTokenDelegatingHandler.ClaveTokenEnCurso);
                    items.Remove(BearerTokenDelegatingHandler.ClaveVencimientoEnCurso);
                }

                _cache.Set(cacheKey, sesionActiva, _cacheDuration);
            }

            if (!sesionActiva)
            {
                _cache.Remove(cacheKey);
                await RechazarSesionAsync(context, MotivosSesion.Inactiva);
            }
        }
        catch (SesionExpiradaException)
        {
            await RechazarSesionAsync(context, MotivosSesion.Vencida);
        }
        catch (ServicioNoDisponibleException exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo validar la sesión del usuario {Sub} en el servicio de Usuarios.",
                sub);

            await RechazarSesionAsync(context, MotivosSesion.NoDisponible);
        }
    }

    private static async Task RechazarSesionAsync(CookieValidatePrincipalContext context, string motivo)
    {
        context.HttpContext.Items[MotivosSesion.ClaveItem] = motivo;
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
