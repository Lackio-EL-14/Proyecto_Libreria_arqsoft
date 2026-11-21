using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Libreria.Servicio.Usuarios.Presentation.Comun;

/// <summary>
/// Errores que no son reglas de negocio (contrato 6.5): si la BD de Usuarios o Keycloak no
/// responden → 503; cualquier otra excepción → 500. Ambos con <see cref="ProblemDetails"/> y sin
/// detalles internos (nunca la traza ni el mensaje original).
/// </summary>
public sealed class ManejadorExcepciones : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ManejadorExcepciones> _logger;

    public ManejadorExcepciones(IProblemDetailsService problemDetails, ILogger<ManejadorExcepciones> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var dependenciaCaida = EsDependenciaCaida(exception);
        _logger.LogError(exception, dependenciaCaida
            ? "La base de datos de Usuarios o Keycloak no responde."
            : "Error inesperado en el servicio de Usuarios.");

        var status = dependenciaCaida ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = dependenciaCaida
                    ? "El servicio de Usuarios no está disponible."
                    : "Ocurrió un error inesperado.",
                Detail = dependenciaCaida
                    ? "No se pudo conectar con la base de datos de usuarios o con el proveedor de identidad. Intente nuevamente en unos minutos."
                    : "No se pudo completar la operación. Intente nuevamente."
            }
        });
    }

    /// <summary>SQL Server sin conexión (red, tiempo agotado) o Keycloak que no responde.</summary>
    public static bool EsDependenciaCaida(Exception exception) =>
        exception is SqlException { Number: -2 or 53 or 10060 or 4060 }
            or HttpRequestException
            or TimeoutException
            or TaskCanceledException;
}
