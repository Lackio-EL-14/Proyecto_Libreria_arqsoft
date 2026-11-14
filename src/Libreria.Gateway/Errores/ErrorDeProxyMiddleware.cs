using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

namespace Libreria.Gateway.Errores;

/// <summary>
/// Eslabón del pipeline del proxy (Chain of Responsibility): deja que YARP reenvíe la solicitud
/// y, si el microservicio no respondió, convierte el error en un 503 con <c>ProblemDetails</c>
/// (sección 6.5: «servicio dependiente caído → 503»). El Front lo traduce a <c>ServicioNoDisponible</c>.
/// </summary>
public sealed class ErrorDeProxyMiddleware
{
    private static readonly HashSet<ForwarderError> ErroresDeServicioCaido =
    [
        ForwarderError.Request,
        ForwarderError.RequestTimedOut,
        ForwarderError.NoAvailableDestinations
    ];

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ErrorDeProxyMiddleware> _logger;

    public ErrorDeProxyMiddleware(RequestDelegate siguiente, ILogger<ErrorDeProxyMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        await _siguiente(contexto);

        var error = contexto.GetForwarderErrorFeature();
        if (error is null || !ErroresDeServicioCaido.Contains(error.Error) || contexto.Response.HasStarted)
        {
            return;
        }

        var servicio = contexto.GetReverseProxyFeature().Route.Cluster?.ClusterId ?? "solicitado";
        _logger.LogWarning(error.Exception,
            "El servicio {Servicio} no respondió a {Metodo} {Ruta} ({Error}).",
            servicio, contexto.Request.Method, contexto.Request.Path, error.Error);

        await EscribirServicioNoDisponibleAsync(contexto, servicio);
    }

    private static Task EscribirServicioNoDisponibleAsync(HttpContext contexto, string servicio) =>
        Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Servicio no disponible",
                detail: $"El servicio de {servicio} no está disponible en este momento.",
                extensions: new Dictionary<string, object?> { ["servicio"] = servicio })
            .ExecuteAsync(contexto);
}
