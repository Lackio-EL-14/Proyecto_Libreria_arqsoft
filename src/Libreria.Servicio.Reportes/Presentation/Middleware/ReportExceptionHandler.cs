using Libreria.Servicio.Reportes.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Libreria.Servicio.Reportes.Presentation.Middleware;

public sealed class ReportExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception is ReportDataUnavailableException ? StatusCodes.Status503ServiceUnavailable
            : exception is UnauthorizedAccessException ? StatusCodes.Status401Unauthorized
            : StatusCodes.Status500InternalServerError;
        context.Response.StatusCode = status;
        await Results.Problem(statusCode: status,
            title: status == 503 ? "No se pudo consultar la información del reporte."
                : status == 401 ? "Se requiere el usuario de la sesión."
                : "No se pudo generar el reporte.",
            detail: status == 503 ? "Verifique la configuración y disponibilidad de la base de datos."
                : "Revise la solicitud o contacte al administrador.").ExecuteAsync(context);
        return true;
    }
}
