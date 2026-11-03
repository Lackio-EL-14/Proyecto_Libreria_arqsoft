using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Facades;
using Libreria.Servicio.Reportes.Application.UseCases;
using Libreria.Servicio.Reportes.Domain.Reports;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Libreria.Servicio.Reportes.Application.Abstractions;

namespace Libreria.Servicio.Reportes.Presentation.Controllers;

[ApiController]
[Route("api/reportes")]
public sealed class ReportesController : ControllerBase
{
    [HttpGet("ventas")]
    public async Task<ActionResult<SalesReportResult>> ReadSales(
        [FromQuery] SalesReportFilter filter, [FromServices] ReporteFacade facade, CancellationToken cancellationToken) =>
        Ok(await facade.ReadSalesAsync(filter, cancellationToken));

    [HttpGet("ventas/pdf")]
    public async Task<IActionResult> ExportSalesPdf(
        [FromQuery] SalesReportFilter filter, [FromServices] ReporteFacade facade, CancellationToken cancellationToken)
    {
        var result = await facade.ExportSalesPdfAsync(filter, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("ventas/exportar")]
    public async Task<IActionResult> ExportSales(
        [FromQuery] SalesReportFilter filter,
        [FromQuery, EnumDataType(typeof(ReportFormat))] ReportFormat format,
        [FromServices] ReporteFacade facade,
        CancellationToken cancellationToken)
    {
        var result = await facade.ExportSalesAsync(
            filter,
            format,
            cancellationToken);

        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpGet("categorias")]
    public async Task<ActionResult<IReadOnlyList<ReportCategory>>> ReadCategories(
        [FromServices] IProductReportRepository repository, CancellationToken cancellationToken) =>
        Ok(await repository.ReadCategoriesAsync(cancellationToken));

    [HttpGet("productos")]
    public async Task<ActionResult<ReportDocument>> ReadProducts(
        [FromQuery] ProductReportFilter filter,
        [FromServices] ReadProductReportUseCase useCase, CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(filter, cancellationToken));

    [HttpGet("productos/exportar")]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] ProductReportFilter filter,
        [FromQuery, EnumDataType(typeof(ReportFormat))] ReportFormat format,
        [FromServices] ReporteFacade facade, CancellationToken cancellationToken)
    {
        var result = await facade.ExportProductsAsync(filter, format, cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

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
