using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Libreria.Servicio.Productos.Presentation.Comun;

/// <summary>
/// Errores que no son reglas de negocio (US-139, contrato 6.5): si MongoDB no responde → 503,
/// cualquier otra excepción → 500. Ambos con <see cref="ProblemDetails"/> y sin detalles internos.
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
        var bdCaida = EsBaseDeDatosCaida(exception);
        _logger.LogError(exception, bdCaida
            ? "La base de datos de Productos no responde."
            : "Error inesperado en el servicio de Productos.");

        var status = bdCaida ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = bdCaida
                    ? "El servicio de Productos no está disponible."
                    : "Ocurrió un error inesperado.",
                Detail = bdCaida
                    ? "No se pudo conectar con la base de datos de productos. Intente nuevamente en unos minutos."
                    : "No se pudo completar la operación. Intente nuevamente."
            }
        });
    }

    /// <summary>
    /// Sin conexión con MongoDB: conexión rechazada o perdida, o tiempo agotado al elegir servidor
    /// (el driver lanza <see cref="TimeoutException"/> cuando el contenedor está apagado).
    /// </summary>
    public static bool EsBaseDeDatosCaida(Exception exception) =>
        exception is MongoConnectionException or TimeoutException;
}
