using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Facades;
using Libreria.Servicio.Reportes.Application.Factories;
using Libreria.Servicio.Reportes.Application.UseCases;
using Libreria.Servicio.Reportes.Domain.Reports;
using Libreria.Servicio.Reportes.Infrastructure.Charts;
using Libreria.Servicio.Reportes.Infrastructure.Export;
using Libreria.Servicio.Reportes.Infrastructure.Factories;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class FacadeAndGeneratorTests
{
    private static ReadProductReportUseCase CreateProductUseCase() =>
        new(new ProductSampleRepository(), new TestUser(), new FixedClock());

    [Fact]
    public async Task FacadeCoordinatesReadPreparationFactoryAndExport()
    {
        var calls = new List<string>();
        var repository = new SampleRepository(() => calls.Add("query"));
        var factory = new RecordingFactory(calls);
        var facade = new ReporteFacade(new ReadExampleReportUseCase(repository, new TestUser(), new FixedClock()),
            new RecordingChart(calls), [factory], CreateProductUseCase());

        var result = await facade.ExportExampleAsync(ReportFormat.Pdf, CancellationToken.None);

        Assert.Equal(new[] { "query", "chart", "factory", "export" }, calls);
        Assert.Equal("mauricio", factory.Document!.GeneratedBy);
        Assert.Equal(FixedClock.Now, factory.Document.GeneratedAt);
        Assert.Equal(repository.Rows, factory.Document.Rows);
        Assert.Equal(new byte[] { 9 }, factory.ChartPng);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Content);
    }

    [Fact]
    public async Task InvalidFormatDoesNotQueryDatabase()
    {
        var calls = new List<string>();
        var readExample = new ReadExampleReportUseCase(new SampleRepository(() => calls.Add("query")),
            new TestUser(), new FixedClock());
        var facade = new ReporteFacade(readExample, new RecordingChart(calls), [], CreateProductUseCase());
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            facade.ExportExampleAsync((ReportFormat)999, CancellationToken.None));
        Assert.Empty(calls);
    }

    [Fact]
    public async Task MissingIdentityDoesNotQueryDatabase()
    {
        var calls = new List<string>();
        var readExample = new ReadExampleReportUseCase(new SampleRepository(() => calls.Add("query")),
            new TestUser(null, null), new FixedClock());
        var facade = new ReporteFacade(readExample, new RecordingChart(calls), [new PdfReportGeneratorFactory()], CreateProductUseCase());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => facade.ExportExampleAsync(ReportFormat.Pdf, CancellationToken.None));
        Assert.Empty(calls);
    }

    [Fact]
    public void ConcreteCreatorsProduceTheirOwnGenerators()
    {
        Assert.IsType<PdfReportGenerator>(new PdfReportGeneratorFactory().CreateGenerator());
        Assert.IsType<ExcelReportGenerator>(new ExcelReportGeneratorFactory().CreateGenerator());
        Assert.Equal(ReportFormat.Pdf, new PdfReportGeneratorFactory().Format);
        Assert.Equal(ReportFormat.Excel, new ExcelReportGeneratorFactory().Format);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Offline")]
    public void RealLibrariesGeneratePdfExcelAndPngLocally(bool empty)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        IReadOnlyList<ReportRow> rows = empty ? [] : [new("Cuaderno ñ", 12.50m), new("=1+1", 8.25m)];
        var png = new ScottPlotChartRenderer().Render(rows);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png[..8]);
        var document = new ReportDocument("Ejemplo US-99", rows, "mauricio", FixedClock.Now);
        var pdf = new PdfReportGeneratorFactory().CreateGenerator().Generate(document, png);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf.Content[..8]));
        Assert.Contains("%%EOF", Encoding.ASCII.GetString(pdf.Content[^100..]));
        Assert.Equal("application/pdf", pdf.ContentType);
        using var parsedPdf = UglyToad.PdfPig.PdfDocument.Open(pdf.Content);
        Assert.Equal(1, parsedPdf.NumberOfPages);
        Assert.Contains("mauricio", parsedPdf.GetPage(1).Text);
        Assert.True(parsedPdf.GetPage(1).NumberOfImages > 0);

        var excel = new ExcelReportGeneratorFactory().CreateGenerator().Generate(document, png);
        using var archive = new ZipArchive(new MemoryStream(excel.Content));
        Assert.NotNull(archive.GetEntry("xl/workbook.xml"));
        Assert.Single(archive.Entries, entry => entry.FullName.StartsWith("xl/media/"));
        using var workbook = new XLWorkbook(new MemoryStream(excel.Content));
        var sheet = workbook.Worksheet(1);
        Assert.Equal("mauricio", sheet.Cell(2, 2).GetString());
        Assert.Single(sheet.Pictures);
        if (!empty)
        {
            Assert.Equal(12.50m, sheet.Cell(6, 2).GetValue<decimal>());
            Assert.Equal("=1+1", sheet.Cell(7, 1).GetString());
            Assert.False(sheet.Cell(7, 1).HasFormula);
        }

        var output = Environment.GetEnvironmentVariable("LIBRERIA_REPORTES_ARTIFACTS");
        if (!string.IsNullOrWhiteSpace(output))
        {
            Directory.CreateDirectory(output);
            var suffix = empty ? "empty" : "sample";
            File.WriteAllBytes(Path.Combine(output, suffix + ".pdf"), pdf.Content);
            File.WriteAllBytes(Path.Combine(output, suffix + ".xlsx"), excel.Content);
            File.WriteAllBytes(Path.Combine(output, suffix + ".png"), png);
        }
    }

    private sealed class RecordingChart(List<string> calls) : IReportChartRenderer
    {
        public byte[] Render(IReadOnlyList<ReportRow> rows) { calls.Add("chart"); return [9]; }
    }

    private sealed class RecordingFactory(List<string> calls) : ReportGeneratorFactory
    {
        public ReportDocument? Document { get; private set; }
        public byte[]? ChartPng { get; private set; }
        public override ReportFormat Format => ReportFormat.Pdf;
        public override IReportGenerator CreateGenerator()
        {
            calls.Add("factory");
            return new RecordingGenerator((document, chartPng) =>
            {
                Document = document;
                ChartPng = chartPng;
                calls.Add("export");
            });
        }
    }

    private sealed class RecordingGenerator(Action<ReportDocument, byte[]> capture) : IReportGenerator
    {
        public ExportedReport Generate(ReportDocument document, byte[] chartPng)
        {
            capture(document, chartPng);
            return new([1, 2, 3], "application/pdf", "test.pdf");
        }
    }
}

internal sealed class TestUser(int? id = 1, string? username = "mauricio") : ICurrentUser
{
    public int? UserId => id;
    public string? Username => username;
}

internal sealed class FixedClock : TimeProvider
{
    internal static readonly DateTimeOffset Now = new(2026, 11, 1, 16, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class SampleRepository(Action? onRead = null) : IExampleReportRepository
{
    internal IReadOnlyList<ReportRow> Rows { get; } = [new("Cuaderno", 12.50m), new("Lápiz", 3.25m)];
    public Task<IReadOnlyList<ReportRow>> ReadSampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        onRead?.Invoke();
        return Task.FromResult(Rows);
    }
}
