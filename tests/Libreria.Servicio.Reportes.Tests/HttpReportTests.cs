using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.Exceptions;
using Libreria.Servicio.Reportes.Domain.Reports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class HttpReportTests
{
    [Fact]
    public async Task JsonUsesHeaderIdentityAndDoesNotExposeInternalIds()
    {
        await using var app = new ReportTestApplication();
        using var client = app.CreateClient();
        AddIdentity(client);
        using var response = await client.GetAsync("/api/reportes/ejemplo");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.Equal("mauricio", document.RootElement.GetProperty("generatedBy").GetString());
        Assert.Equal(2, document.RootElement.GetProperty("rows").GetArrayLength());
        Assert.DoesNotContain("userId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("productoId", json, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Pdf", "application/pdf", ".pdf")]
    [InlineData("Excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx")]
    public async Task HttpExportsRealFiles(string format, string contentType, string extension)
    {
        await using var app = new ReportTestApplication();
        using var client = app.CreateClient();
        AddIdentity(client);
        using var response = await client.PostAsJsonAsync("/api/reportes/ejemplo/exportar", new { format });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType!.MediaType);
        Assert.Contains(extension, response.Content.Headers.ContentDisposition!.FileName);
        Assert.True((await response.Content.ReadAsByteArrayAsync()).Length > 1000);
    }

    [Fact]
    public async Task MissingHeadersReturn401ProblemDetails()
    {
        await using var app = new ReportTestApplication();
        using var client = app.CreateClient();
        await AssertProblem(client, "GET", null, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("0", "mauricio")]
    [InlineData("-1", "mauricio")]
    [InlineData("abc", "mauricio")]
    [InlineData("1", " ")]
    [InlineData("1", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task InvalidIdentityReturns400(string id, string name)
    {
        await using var app = new ReportTestApplication();
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Usuario-Id", id);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Usuario-Nombre", name);
        await AssertProblem(client, "GET", null, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"format\":\"Csv\"}")]
    [InlineData("{\"format\":999}")]
    [InlineData("{\"format\":null}")]
    public async Task InvalidJsonFormatReturns400(string json)
    {
        await using var app = new ReportTestApplication();
        using var client = app.CreateClient();
        AddIdentity(client);
        await AssertProblem(client, "POST", json, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DuplicateIdentityHeadersAreRejected()
    {
        await using var app = new ReportTestApplication();
        using var client = app.CreateClient();
        AddIdentity(client);
        client.DefaultRequestHeaders.Add("X-Usuario-Id", "2");
        await AssertProblem(client, "GET", null, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DatabaseFailureReturns503WithoutLeakingDetails()
    {
        await using var app = new ReportTestApplication(new FailingRepository());
        using var client = app.CreateClient();
        AddIdentity(client);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/pdf");
        using var response = await client.GetAsync("/api/reportes/ejemplo");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        Assert.DoesNotContain("SecretDatabase", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WeatherForecastIsRemovedAndSwaggerDescribesReportRoutes()
    {
        await using var app = new ReportTestApplication();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/weatherforecast")).StatusCode);
        var json = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/reportes/ejemplo/exportar", json);
    }

    private static void AddIdentity(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("X-Usuario-Id", "1");
        client.DefaultRequestHeaders.Add("X-Usuario-Nombre", "mauricio");
    }

    private static async Task AssertProblem(HttpClient client, string method, string? json, HttpStatusCode status)
    {
        using var response = method == "GET" ? await client.GetAsync("/api/reportes/ejemplo")
            : await client.PostAsync("/api/reportes/ejemplo/exportar", new StringContent(json!, Encoding.UTF8, "application/json"));
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
    }

    private sealed class FailingRepository : IExampleReportRepository
    {
        public Task<IReadOnlyList<ReportRow>> ReadSampleAsync(CancellationToken cancellationToken) =>
            throw new ReportDataUnavailableException("SecretDatabase connection failure");
    }
}

internal sealed class ReportTestApplication(IExampleReportRepository? repository = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExampleReportRepository>();
            services.AddScoped<IExampleReportRepository>(_ => repository ?? new SampleRepository());
        });
    }
}
