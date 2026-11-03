using System.Net;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class SalesReportFrontTests
{
    private const string Filters = "Filter.DateFrom=2026-11-01&Filter.DateTo=2026-11-01&Filter.CategoryPublicId=11111111-1111-1111-1111-111111111111";

    [Fact]
    public async Task FrontSendsFiltersThroughHttpRendersTotalsAndDownloadsPdf()
    {
        var repository = new SalesSampleRepository();
        await using var service = new ReportTestApplication(sales: repository);
        await using var front = new ReportFrontApplication(service);
        using var client = front.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Reportes/Ventas?" + Filters));
        foreach (var label in new[] { "Reporte de ventas", "Cuaderno", "Papelería", "Total de unidades vendidas: 5", "Total vendido: Bs 65,00", "Descargar PDF" })
            Assert.Contains(label, html);
        Assert.DoesNotContain("<td>Agenda</td>", html);
        Assert.DoesNotContain("Descargar Excel", html);
        Assert.Equal(new DateOnly(2026, 11, 1), repository.LastFilter!.DateFrom);
        Assert.Equal(new DateOnly(2026, 11, 1), repository.LastFilter.DateTo);
        Assert.Equal(ProductSampleRepository.CategoryId, repository.LastFilter.CategoryPublicId);
        using var response = await client.GetAsync("/Reportes/Ventas?handler=Pdf&" + Filters);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        using var pdf = UglyToad.PdfPig.PdfDocument.Open(await response.Content.ReadAsByteArrayAsync());
        Assert.Contains("Total vendido: Bs 65,00", pdf.GetPage(1).Text);
        Assert.Contains("Reporte generado: mauricio", pdf.GetPage(1).Text);
        Assert.Contains("/Reportes/Ventas", await client.GetStringAsync("/Reportes"));
    }

    [Theory]
    [InlineData("Filter.DateFrom=2026-11-02&Filter.DateTo=2026-11-01")]
    [InlineData("Filter.DateFrom=incorrecta")]
    [InlineData("Filter.DateTo=2026-02-30")]
    [InlineData("Filter.CategoryPublicId=incorrecta")]
    public async Task InvalidFrontFiltersShowMessageWithoutSalesQuery(string query)
    {
        var repository = new SalesSampleRepository();
        await using var service = new ReportTestApplication(sales: repository);
        await using var front = new ReportFrontApplication(service);
        using var client = front.CreateClient();
        using var response = await client.GetAsync("/Reportes/Ventas?handler=Pdf&" + query);
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        Assert.Contains("Revise las fechas y la categoría", html);
        Assert.Null(repository.LastFilter);
    }

    [Fact]
    public async Task EmptyUnknownCategoryAndUnavailableServiceShowClearMessages()
    {
        await using var service = new ReportTestApplication();
        await using var front = new ReportFrontApplication(service);
        using var client = front.CreateClient();
        var empty = await client.GetStringAsync("/Reportes/Ventas?Filter.DateFrom=2026-11-03");
        Assert.Contains("No se encontraron ventas", empty);
        Assert.Contains("Total de unidades vendidas: 0", empty);
        var invalid = WebUtility.HtmlDecode(await client.GetStringAsync("/Reportes/Ventas?Filter.CategoryPublicId=22222222-2222-2222-2222-222222222222"));
        Assert.Contains("categoría activa existente", invalid);
        await using var unavailable = new ReportTestApplication(sales: new UnavailableSalesRepository());
        await using var unavailableFront = new ReportFrontApplication(unavailable);
        using var unavailableClient = unavailableFront.CreateClient();
        var error = await unavailableClient.GetStringAsync("/Reportes/Ventas?handler=Pdf");
        Assert.Contains("servicio de Reportes", error);
    }
}
