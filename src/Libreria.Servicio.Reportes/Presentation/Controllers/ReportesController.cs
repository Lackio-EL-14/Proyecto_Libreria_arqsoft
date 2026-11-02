using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Facades;
using Libreria.Servicio.Reportes.Application.UseCases;
using Libreria.Servicio.Reportes.Domain.Reports;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Reportes.Presentation.Controllers;

[ApiController]
[Route("api/reportes")]
public sealed class ReportesController : ControllerBase
{
    [HttpGet("ejemplo")]
    [ProducesResponseType<ReportDocument>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportDocument>> ReadExample(
        [FromServices] ReadExampleReportUseCase useCase, CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpPost("ejemplo/exportar")]
    public async Task<IActionResult> ExportExample(
        [FromBody] ExportExampleRequest request,
        [FromServices] ReporteFacade facade, CancellationToken cancellationToken)
    {
        var result = await facade.ExportExampleAsync(request.Format!.Value, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }
}
