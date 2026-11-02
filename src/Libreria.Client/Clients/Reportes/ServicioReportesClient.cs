using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Reportes;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace Libreria.Client.Clients.Reportes;

public sealed class ServicioReportesClient : ApiClientBase, IServicioReportesClient
{
    private readonly HttpClient _httpClient;
    public ServicioReportesClient(HttpClient httpClient)
        : base(httpClient, "Reportes")
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ReportCategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<ReportCategoryDto>>("api/reportes/categorias", cancellationToken) ?? [];

    public Task<ProductReportDto?> GetProductsAsync(ProductReportFilterDto filter, CancellationToken cancellationToken = default) =>
        GetAsync<ProductReportDto>(BuildUrl("api/reportes/productos", filter), cancellationToken);

    public Task<SalesReportDto?> GetSalesAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default) =>
        GetAsync<SalesReportDto>(BuildSalesUrl("api/reportes/ventas", filter), cancellationToken);

    public Task<ReportFileDto> DownloadSalesPdfAsync(SalesReportFilterDto filter, CancellationToken cancellationToken = default) =>
        DownloadFileAsync(BuildSalesUrl("api/reportes/ventas/pdf", filter), "reporte-ventas.pdf", cancellationToken);

    public async Task<ReportFileDto> DownloadProductsAsync(
        ProductReportFilterDto filter, string format, CancellationToken cancellationToken = default)
    {
        if (format is not ("Pdf" or "Excel"))
            throw new ArgumentException("El formato debe ser PDF o Excel.", nameof(format));

        var url = QueryHelpers.AddQueryString(BuildUrl("api/reportes/productos/exportar", filter), "format", format);
        return await DownloadFileAsync(url, format == "Pdf" ? "reporte-productos.pdf" : "reporte-productos.xlsx", cancellationToken);
    }

    private async Task<ReportFileDto> DownloadFileAsync(string url, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                ApiProblemDetailsDto? problem = null;
                try { problem = await response.Content.ReadFromJsonAsync<ApiProblemDetailsDto>(cancellationToken); }
                catch (JsonException) { }
                throw new ApiClientException("Reportes", response.StatusCode,
                    problem?.Detail ?? problem?.Title ?? "No se pudo descargar el reporte.");
            }
            return new(await response.Content.ReadAsByteArrayAsync(cancellationToken),
                response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                fileName);
        }
        catch (HttpRequestException exception) { throw new ServicioNoDisponibleException("Reportes", exception); }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        { throw new ServicioNoDisponibleException("Reportes", exception); }
    }

    private static string BuildUrl(string path, ProductReportFilterDto filter) =>
        QueryHelpers.AddQueryString(path, new Dictionary<string, string?>
        {
            ["CategoryPublicId"] = filter.CategoryPublicId?.ToString(),
            ["MinimumPrice"] = filter.MinimumPrice?.ToString(CultureInfo.InvariantCulture),
            ["MaximumPrice"] = filter.MaximumPrice?.ToString(CultureInfo.InvariantCulture)
        });

    private static string BuildSalesUrl(string path, SalesReportFilterDto filter) =>
        QueryHelpers.AddQueryString(path, new Dictionary<string, string?>
        {
            ["DateFrom"] = filter.DateFrom?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["DateTo"] = filter.DateTo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["CategoryPublicId"] = filter.CategoryPublicId?.ToString()
        });
}
