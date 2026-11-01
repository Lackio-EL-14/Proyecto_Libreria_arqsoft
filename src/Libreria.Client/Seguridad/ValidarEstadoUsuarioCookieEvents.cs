using System.Security.Claims;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Usuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Libreria.Client.Seguridad;

public class ValidarEstadoUsuarioCookieEvents : CookieAuthenticationEvents
{
    private const string CacheKeyPrefix = "sesion-activa:";

    private readonly IServicioUsuariosClient _usuariosClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ValidarEstadoUsuarioCookieEvents> _logger;
    private readonly TimeSpan _cacheDuration;

    public ValidarEstadoUsuarioCookieEvents(
        IServicioUsuariosClient usuariosClient,
        IMemoryCache cache,
        IOptions<SesionActivaOptions> options,
        ILogger<ValidarEstadoUsuarioCookieEvents> logger)
    {
        _usuariosClient = usuariosClient;
        _cache = cache;
        _logger = logger;
        _cacheDuration = TimeSpan.FromSeconds(
            Math.Clamp(options.Value.ValidacionEstadoSegundos, 5, 60));
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var identificador = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(identificador, out var usuarioId))
        {
            await RechazarSesionAsync(context);
            return;
        }

        var cacheKey = $"{CacheKeyPrefix}{usuarioId}";

        try
        {
            if (!_cache.TryGetValue(cacheKey, out bool sesionActiva))
            {
                sesionActiva = await _usuariosClient.SesionActivaAsync(
                    usuarioId,
                    context.HttpContext.RequestAborted);

                _cache.Set(cacheKey, sesionActiva, _cacheDuration);
            }

            if (!sesionActiva)
            {
                _cache.Remove(cacheKey);
                await RechazarSesionAsync(context);
            }
        }
        catch (ServicioNoDisponibleException exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo validar la sesión del usuario {UsuarioId} en el servicio de Usuarios.",
                usuarioId);

            await RechazarSesionAsync(context);
        }
    }

    private static async Task RechazarSesionAsync(
        CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(context.Scheme.Name);
    }
}
