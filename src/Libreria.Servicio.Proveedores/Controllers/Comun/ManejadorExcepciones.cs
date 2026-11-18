using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Libreria.Servicio.Proveedores.Controllers.Comun;

public sealed class ManejadorExcepciones : IExceptionHandler
{
    private readonly IProblemDetailsService _problemas;
    private readonly ILogger<ManejadorExcepciones> _logger;

    public ManejadorExcepciones(IProblemDetailsService problemas, ILogger<ManejadorExcepciones> logger)
    {
        _problemas = problemas;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        var bdCaida = excepcion is SqlException sql
            && (sql.IsTransient || sql.Number is -2 or 53 or 10060);

        _logger.LogError(
            excepcion,
            bdCaida ? "La BD de Proveedores no responde." : "Error inesperado en Proveedores.");
        contexto.Response.StatusCode = bdCaida ? 503 : 500;
        return await _problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = excepcion,
            ProblemDetails = new ProblemDetails
            {
                Status = contexto.Response.StatusCode,
                Title = bdCaida
                    ? "El servicio de Proveedores no está disponible."
                    : "Ocurrió un error inesperado.",
                Detail = bdCaida
                    ? "No se pudo conectar con la base de datos de proveedores."
                    : "No se pudo completar la operación."
            }
        });
    }
}
