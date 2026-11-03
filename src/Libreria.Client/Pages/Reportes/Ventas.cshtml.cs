using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Reportes;
using Libreria.Client.DTOs.Reportes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Pages.Reportes;

public sealed class SalesReportModel(IServicioReportesClient reports) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public SalesReportFilterDto Filter { get; set; } = new();
    public IReadOnlyList<ReportCategoryDto> Categories { get; private set; } = [];
    public SalesReportDto? Report { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Categories = await reports.GetCategoriesAsync(cancellationToken);
            if (ModelState.IsValid)
                Report = await reports.GetSalesAsync(Filter, cancellationToken);
        }
        catch (ApiClientException exception) when (exception.StatusCode == System.Net.HttpStatusCode.BadRequest)
        { ErrorMessage = "Revise las fechas y seleccione una categoría activa existente."; }
        catch (Exception exception) when (exception is ApiClientException or ServicioNoDisponibleException)
        { ErrorMessage = "No se pudo consultar el reporte. Verifique que el servicio de Reportes esté disponible."; }
    }

    public async Task<IActionResult> OnGetPdfAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await OnGetAsync(cancellationToken); return Page(); }
        try
        {
            var file = await reports.DownloadSalesPdfAsync(Filter, cancellationToken);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (Exception exception) when (exception is ApiClientException or ServicioNoDisponibleException)
        {
            await OnGetAsync(cancellationToken);
            ErrorMessage ??= "No se pudo descargar el PDF. Verifique que el servicio de Reportes esté disponible.";
            return Page();
        }
    }
}
