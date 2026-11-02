using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using ClosedXML.Excel;
using Libreria.Client.Clients.Reportes;
using Libreria.Client.Pages.Reportes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class ProductReportFrontTests
{
    private const string Filters = "Filter.CategoryPublicId=11111111-1111-1111-1111-111111111111&Filter.MinimumPrice=10.25&Filter.MaximumPrice=15";

    [Fact]
    public async Task RazorPageSendsFiltersAndRendersProductsAndDownloadButtons()
    {
        var repository = new ProductSampleRepository();
        await using var service = new ReportTestApplication(products: repository);
        await using var front = new ReportFrontApplication(service);
        using var client = front.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/Reportes?" + Filters));
        Assert.Contains("Reporte de productos", html);
        Assert.Contains("Cuaderno", html);
        Assert.Contains("Papelería", html);
        Assert.Contains("Escolar", html);
        Assert.DoesNotContain("<td>Lápiz</td>", html);
        Assert.Contains("value=\"Pdf\"", html);
        Assert.Contains("value=\"Excel\"", html);
        Assert.Equal(ProductSampleRepository.CategoryId, repository.LastFilter!.CategoryPublicId);
        Assert.Equal(10.25m, repository.LastFilter.MinimumPrice);
        Assert.Equal(15m, repository.LastFilter.MaximumPrice);
        // Que el servicio acepte estas llamadas también verifica el handler de identidad del Front.
    }

    [Theory]
    [InlineData("Pdf", "application/pdf")]
    [InlineData("Excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task RazorDownloadHandlersReturnRealFilteredFiles(string handler, string contentType)
    {
        await using var service = new ReportTestApplication();
        await using var front = new ReportFrontApplication(service);
        using var client = front.CreateClient();
        using var response = await client.GetAsync($"/Reportes?handler={handler}&{Filters}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType!.MediaType);
        Assert.Contains("reporte-productos", response.Content.Headers.ContentDisposition!.FileName);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        if (handler == "Pdf")
        {
            using var pdf = UglyToad.PdfPig.PdfDocument.Open(bytes);
            var text = string.Concat(pdf.GetPages().Select(page => page.Text));
            Assert.Contains("Cuaderno", text);
            Assert.Contains("mauricio", text);
            Assert.DoesNotContain("Agenda", text);
        }
        else
        {
            using var workbook = new XLWorkbook(new MemoryStream(bytes));
            Assert.Equal("Cuaderno", workbook.Worksheet(1).Cell(6, 1).GetString());
            Assert.Equal(12.50m, workbook.Worksheet(1).Cell(6, 4).GetValue<decimal>());
        }
    }

    [Fact]
    public async Task RazorShowsSpanishMessagesForInvalidFiltersAndEmptyResults()
    {
        await using var service = new ReportTestApplication();
        await using var front = new ReportFrontApplication(service);
        using var client = front.CreateClient();
        foreach (var query in new[] { "Filter.MinimumPrice=-1", "Filter.MinimumPrice=20&Filter.MaximumPrice=10", "Filter.MinimumPrice=abc" })
        {
            using var response = await client.GetAsync("/Reportes?handler=Pdf&" + query);
            Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
            var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
            Assert.Contains("Los precios deben ser números no negativos", html);
        }
        var empty = WebUtility.HtmlDecode(await client.GetStringAsync("/Reportes?Filter.MinimumPrice=100"));
        Assert.Contains("No se encontraron productos", empty);
    }
}

// Solo para pruebas: Front y API se ejecutan realmente en TestServer; únicamente SQL se simula.
internal sealed class ReportFrontApplication(ReportTestApplication reports) : WebApplicationFactory<IndexModel>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        // El módulo antiguo de Usuarios exige una cadena al arrancar; no se abre esta conexión.
        builder.UseSetting("DB_CONNECTION_STRING", "Server=unused;Database=ReportsFrontTests;Integrated Security=true;TrustServerCertificate=true");
        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "ReportsTest";
                options.DefaultChallengeScheme = "ReportsTest";
            }).AddScheme<AuthenticationSchemeOptions, ReportTestAuthentication>("ReportsTest", _ => { });
            services.AddHttpClient<IServicioReportesClient, ServicioReportesClient>()
                .ConfigurePrimaryHttpMessageHandler(() => reports.Server.CreateHandler());
        });
    }
}

internal sealed class ReportTestAuthentication(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Name, "mauricio"),
            new Claim(ClaimTypes.Role, "Administrador")], Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
