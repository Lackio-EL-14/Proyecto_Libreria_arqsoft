using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Libreria.Servicio.Clientes.Controllers.Comun;

/// <summary>
/// Errores que no son reglas de negocio (contrato 6.5): si la BD de Clientes no responde → 503,
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
            ? "La base de datos de Clientes no responde."
            : "Error inesperado en el servicio de Clientes.");

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
                    ? "El servicio de Clientes no está disponible."
                    : "Ocurrió un error inesperado.",
                Detail = bdCaida
                    ? "No se pudo conectar con la base de datos de clientes. Intente nuevamente en unos minutos."
                    : "No se pudo completar la operación. Intente nuevamente."
            }
        });
    }

    /// <summary>Sin conexión con MySQL (servidor apagado, red, tiempo agotado).</summary>
    public static bool EsBaseDeDatosCaida(Exception exception) =>
        exception is MySqlException { ErrorCode: MySqlErrorCode.UnableToConnectToHost }
            or MySqlException { IsTransient: true };
}
