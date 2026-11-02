using System.Net;
using System.Net.Http.Json;
using ClosedXML.Excel;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class ProductReportTests
{
    [Theory]
    [InlineData("", 3)]
    [InlineData("CategoryPublicId=11111111-1111-1111-1111-111111111111", 2)]
    [InlineData("MinimumPrice=10&MaximumPrice=20", 2)]
    [InlineData("CategoryPublicId=11111111-1111-1111-1111-111111111111&MinimumPrice=10&MaximumPrice=15", 1)]
    [InlineData("MinimumPrice=100", 0)]
    public async Task HttpPassesFiltersAndReturnsOrderedData(string query, int count)
    {
        var repository = new ProductSampleRepository();
        await using var app = new ReportTestApplication(products: repository);
        using var client = CreateClient(app);
        var document = await client.GetFromJsonAsync<ReportDocument>("/api/reportes/productos?" + query);
        Assert.Equal(count, document!.Rows.Count);
        Assert.Equal("Reporte de productos", document.Title);
        Assert.Equal("mauricio", document.GeneratedBy);
        Assert.True(document.IsProductReport);
        Assert.Equal(document.Rows.OrderBy(row => row.Name).ToArray(), document.Rows);
        Assert.NotNull(repository.LastFilter);
        if (query.Contains("CategoryPublicId")) Assert.Equal(ProductSampleRepository.CategoryId, repository.LastFilter.CategoryPublicId);
        if (query.Contains("MinimumPrice=10&")) Assert.Equal(10m, repository.LastFilter.MinimumPrice);
    }

    [Theory]
    [InlineData("MinimumPrice=-1")]
    [InlineData("MaximumPrice=-1")]
    [InlineData("MinimumPrice=20&MaximumPrice=10")]
    [InlineData("MinimumPrice=abc")]
    [InlineData("CategoryPublicId=abc")]
    [InlineData("CategoryPublicId=00000000-0000-0000-0000-000000000000")]
    public async Task InvalidFiltersAreRejectedBeforeQuery(string query)
    {
        var repository = new ProductSampleRepository();
        await using var app = new ReportTestApplication(products: repository);
        using var client = CreateClient(app);
        foreach (var path in new[] { "/api/reportes/productos?", "/api/reportes/productos/exportar?format=Pdf&" })
        {
            using var response = await client.GetAsync(path + query);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        }
        Assert.Null(repository.LastFilter);
    }

    [Theory]
    [InlineData("Pdf", false)]
    [InlineData("Excel", false)]
    [InlineData("Pdf", true)]
    [InlineData("Excel", true)]
    public async Task ExportsContainFilteredProductsAndMetadata(string format, bool empty)
    {
        await using var app = new ReportTestApplication();
        using var client = CreateClient(app);
        var query = empty ? "MinimumPrice=100" : "MinimumPrice=10&MaximumPrice=15";
        using var response = await client.GetAsync($"/api/reportes/productos/exportar?format={format}&{query}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (format == "Pdf")
        {
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(bytes);
            var text = string.Concat(pdf.GetPages().Select(page => page.Text));
            Assert.Contains("Reporte de productos", text);
            Assert.Contains("mauricio", text);
            Assert.Contains("UTC", text);
            Assert.Contains("Categoría", text);
            Assert.Contains(empty ? "No se encontraron productos" : "Cuaderno", text);
            Assert.DoesNotContain("Lápiz", text);
        }
        else
        {
            using var workbook = new XLWorkbook(new MemoryStream(bytes));
            var sheet = workbook.Worksheet(1);
            Assert.Equal("Reporte de productos", sheet.Cell(1, 1).GetString());
            Assert.Equal("mauricio", sheet.Cell(2, 2).GetString());
            Assert.Equal(XLDataType.DateTime, sheet.Cell(3, 2).DataType);
            Assert.Equal("Categoría", sheet.Cell(5, 2).GetString());
            Assert.Equal("Marca", sheet.Cell(5, 3).GetString());
            Assert.Empty(sheet.Pictures);
            if (empty) Assert.Contains("No se encontraron", sheet.Cell(6, 1).GetString());
            else
            {
                Assert.Equal("Cuaderno", sheet.Cell(6, 1).GetString());
                Assert.Equal("Papelería", sheet.Cell(6, 2).GetString());
                Assert.Equal("Escolar", sheet.Cell(6, 3).GetString());
                Assert.Equal(12.50m, sheet.Cell(6, 4).GetValue<decimal>());
                Assert.True(sheet.Cell(7, 1).IsEmpty());
            }
        }
    }

    [Fact]
    public async Task CategoryOptionsUsePublicIdsAndExportRejectsUnknownFormat()
    {
        await using var app = new ReportTestApplication();
        using var client = CreateClient(app);
        var categories = await client.GetFromJsonAsync<ReportCategory[]>("/api/reportes/categorias");
        Assert.Equal(ProductSampleRepository.CategoryId, categories![0].PublicId);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/reportes/productos/exportar?format=999")).StatusCode);
    }

    private static HttpClient CreateClient(ReportTestApplication app)
    {
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Usuario-Id", "1");
        client.DefaultRequestHeaders.Add("X-Usuario-Nombre", "mauricio");
        return client;
    }
}

// Simula datos únicamente en pruebas; no sustituye la ejecución real de SQL Server.
internal sealed class ProductSampleRepository : IProductReportRepository
{
    internal static readonly Guid CategoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    internal ProductReportFilter? LastFilter { get; private set; }
    public Task<IReadOnlyList<ReportCategory>> ReadCategoriesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ReportCategory>>([new(CategoryId, "Papelería")]);

    public Task<IReadOnlyList<ReportRow>> ReadProductsAsync(ProductReportFilter filter, CancellationToken cancellationToken)
    {
        LastFilter = filter;
        var rows = new[] { new ReportRow("Lápiz", 3m, "Papelería", "Escolar"),
            new ReportRow("Cuaderno", 12.50m, "Papelería", "Escolar"), new ReportRow("Agenda", 20m, "Oficina", "Central") };
        return Task.FromResult<IReadOnlyList<ReportRow>>(rows.Where(row =>
            (!filter.CategoryPublicId.HasValue || filter.CategoryPublicId == CategoryId && row.Category == "Papelería") &&
            (!filter.MinimumPrice.HasValue || row.Amount >= filter.MinimumPrice) &&
            (!filter.MaximumPrice.HasValue || row.Amount <= filter.MaximumPrice)).OrderBy(row => row.Name).ToArray());
    }
}
