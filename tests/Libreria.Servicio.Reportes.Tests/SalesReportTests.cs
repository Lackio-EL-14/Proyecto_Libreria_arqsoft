using System.Net;
using System.Net.Http.Json;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Exceptions;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class SalesReportTests
{
    [Theory]
    [InlineData("", 11, 115)]
    [InlineData("DateFrom=2026-11-01&DateTo=2026-11-01", 6, 95)]
    [InlineData("DateFrom=2026-11-02", 4, 12)]
    [InlineData("DateTo=2026-10-31", 1, 8)]
    [InlineData("CategoryPublicId=11111111-1111-1111-1111-111111111111", 9, 77)]
    [InlineData("DateFrom=2026-11-01&DateTo=2026-11-01&CategoryPublicId=11111111-1111-1111-1111-111111111111", 5, 65)]
    [InlineData("DateFrom=2026-11-03", 0, 0)]
    public async Task HttpReturnsGroupedRowsFiltersAndMatchingTotals(string query, long units, int amount)
    {
        var repository = new SalesSampleRepository();
        await using var app = new ReportTestApplication(sales: repository);
        using var client = CreateClient(app);
        var report = await client.GetFromJsonAsync<SalesReportResult>("/api/reportes/ventas?" + query);
        Assert.Equal(units, report!.TotalQuantity);
        Assert.Equal((decimal)amount, report.TotalAmount);
        Assert.Equal(report.Rows.Sum(row => row.Quantity), report.TotalQuantity);
        Assert.Equal(report.Rows.Sum(row => row.Amount), report.TotalAmount);
        Assert.Equal("mauricio", report.GeneratedBy);
        Assert.Equal(report.Rows.OrderBy(row => row.Name).ToArray(), report.Rows);
        Assert.NotNull(repository.LastFilter);
        Assert.Equal(report.Filter.DateFrom, repository.LastFilter.DateFrom);
        Assert.Equal(report.Filter.DateTo, repository.LastFilter.DateTo);
        Assert.Equal(report.Filter.CategoryPublicId, repository.LastFilter.CategoryPublicId);
        if (query.Length == 0)
        {
            var notebook = Assert.Single(report.Rows, row => row.Name == "Cuaderno");
            Assert.Equal(5, notebook.Quantity);
            Assert.Equal(65m, notebook.Amount); // Suma los importes históricos; excluye la venta anulada de 9000 Bs.
        }
    }

    [Theory]
    [InlineData("DateFrom=2026-11-02&DateTo=2026-11-01")]
    [InlineData("DateFrom=incorrecta")]
    [InlineData("DateTo=2026-02-30")]
    [InlineData("CategoryPublicId=incorrecta")]
    [InlineData("CategoryPublicId=00000000-0000-0000-0000-000000000000")]
    [InlineData("CategoryPublicId=22222222-2222-2222-2222-222222222222")]
    public async Task InvalidFiltersReturn400BeforeSalesQuery(string query)
    {
        var repository = new SalesSampleRepository();
        await using var app = new ReportTestApplication(sales: repository);
        using var client = CreateClient(app);
        foreach (var route in new[]
                 {
                     $"ventas?{query}",
                     $"ventas/pdf?{query}",
                     $"ventas/exportar?format=Excel&{query}"
                 })
        {
            using var response = await client.GetAsync($"/api/reportes/{route}");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        }
        Assert.Null(repository.LastFilter);
    }

    [Theory]
    [InlineData(false, "Pdf", "application/pdf", ".pdf")]
    [InlineData(false, "Excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    [InlineData(true, "Pdf", "application/pdf", ".pdf")]
    [InlineData(true, "Excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    public async Task ExportsUseCommonTemplateRangeTotalsIdentityAndChart(
        bool empty,
        string format,
        string contentType,
        string extension)
    {
        await using var app = new ReportTestApplication();
        using var client = CreateClient(app);
        var query = empty ? "DateFrom=2026-11-03" : "DateFrom=2026-11-01&DateTo=2026-11-01&CategoryPublicId=11111111-1111-1111-1111-111111111111";
        using var response = await client.GetAsync(
            $"/api/reportes/ventas/exportar?format={format}&{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType!.MediaType);
        Assert.Contains(extension, response.Content.Headers.ContentDisposition!.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();

        if (format == "Pdf")
        {
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(bytes);
            var text = string.Concat(pdf.GetPages().Select(page => page.Text));
            Assert.Contains("Reporte de ventas", text);
            Assert.Contains(empty ? "Desde: 03/11/2026 - Hasta: sin límite" : "Desde: 01/11/2026 - Hasta: 01/11/2026", text);
            Assert.Contains(empty ? "Total de unidades vendidas: 0" : "Total de unidades vendidas: 5", text);
            Assert.Contains(empty ? "Total vendido: Bs 0,00" : "Total vendido: Bs 65,00", text);
            Assert.Contains("Reporte generado: mauricio", text);
            Assert.Contains("UTC-04:00", text);
            Assert.Contains(empty ? "No se encontraron registros" : "Cuaderno", text);
            Assert.DoesNotContain("9000", text);
            Assert.Equal(2, pdf.GetPage(1).NumberOfImages);
            Assert.Contains("Total vendido", pdf.GetPage(pdf.NumberOfPages).Text);
        }
        else
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook(new MemoryStream(bytes));
            var sheet = workbook.Worksheet(1);
            Assert.Equal(2, sheet.Pictures.Count);
            Assert.Contains(sheet.CellsUsed(), cell =>
                cell.GetString().Contains("Total vendido: Bs"));
        }
    }

    [Fact]
    public async Task LegacyPdfRouteRemainsAvailable()
    {
        await using var app = new ReportTestApplication();
        using var client = CreateClient(app);
        using var response = await client.GetAsync("/api/reportes/ventas/pdf");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task MissingIdentityAndUnavailableDatabaseUseExistingErrors()
    {
        await using var app = new ReportTestApplication(sales: new UnavailableSalesRepository());
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/reportes/ventas")).StatusCode);
        client.DefaultRequestHeaders.Add("X-Usuario-Id", "1");
        client.DefaultRequestHeaders.Add("X-Usuario-Nombre", "mauricio");
        using var response = await client.GetAsync("/api/reportes/ventas");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("SecretServer", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/reportes/ventas/excel")).StatusCode);
    }

    internal static HttpClient CreateClient(ReportTestApplication app)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Usuario-Id", "1");
        client.DefaultRequestHeaders.Add("X-Usuario-Nombre", "mauricio");
        return client;
    }
}

internal sealed class UnavailableSalesRepository : ISalesReportRepository
{
    public Task<IReadOnlyList<SalesReportRow>> ReadSalesAsync(SalesReportFilter filter, CancellationToken cancellationToken) =>
        throw new ReportDataUnavailableException("SecretServer");
}

// Doble de SQL para las pruebas HTTP/Front; no ejecuta ni valida el motor SQL Server.
internal sealed class SalesSampleRepository : ISalesReportRepository
{
    internal SalesReportFilter? LastFilter { get; private set; }
    public Task<IReadOnlyList<SalesReportRow>> ReadSalesAsync(SalesReportFilter filter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LastFilter = filter;
        var sales = new[]
        {
            new { ProductId = 1, Name = "Cuaderno", Category = "Papelería", Date = new DateTime(2026, 11, 1), State = "Activa", Quantity = 2L, Amount = 20m },
            new { ProductId = 1, Name = "Cuaderno", Category = "Papelería", Date = new DateTime(2026, 11, 1).AddDays(1).AddTicks(-1), State = "Activa", Quantity = 3L, Amount = 45m },
            new { ProductId = 1, Name = "Cuaderno", Category = "Papelería", Date = new DateTime(2026, 11, 1), State = "Anulada", Quantity = 100L, Amount = 9000m },
            new { ProductId = 2, Name = "Agenda", Category = "Oficina", Date = new DateTime(2026, 10, 31), State = "Activa", Quantity = 1L, Amount = 8m },
            new { ProductId = 3, Name = "Lápiz", Category = "Papelería", Date = new DateTime(2026, 11, 2), State = "Activa", Quantity = 4L, Amount = 12m },
            new { ProductId = 4, Name = "Carpeta", Category = "Oficina", Date = new DateTime(2026, 11, 1), State = "Activa", Quantity = 1L, Amount = 30m }
        };
        var rows = sales.Where(sale => sale.State == "Activa" &&
            (!filter.DateFrom.HasValue || sale.Date >= filter.DateFrom.Value.ToDateTime(TimeOnly.MinValue)) &&
            (!filter.DateTo.HasValue || sale.Date <= filter.DateTo.Value.ToDateTime(TimeOnly.MaxValue)) &&
            (!filter.CategoryPublicId.HasValue || filter.CategoryPublicId == ProductSampleRepository.CategoryId && sale.Category == "Papelería"))
            .GroupBy(sale => new { sale.ProductId, sale.Name, sale.Category })
            .Select(group => new SalesReportRow(group.Key.Name, group.Key.Category, group.Sum(sale => sale.Quantity), group.Sum(sale => sale.Amount)))
            .OrderBy(row => row.Name).ToArray();
        return Task.FromResult<IReadOnlyList<SalesReportRow>>(rows);
    }
}
