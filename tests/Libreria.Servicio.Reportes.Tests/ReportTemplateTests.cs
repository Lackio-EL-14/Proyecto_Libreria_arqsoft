using ClosedXML.Excel;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.UseCases;
using Libreria.Servicio.Reportes.Domain.Reports;
using Libreria.Servicio.Reportes.Infrastructure.Export;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class ReportTemplateTests
{
    [Fact]
    public async Task ProductTemplateIncludesLogoSummaryIdentityTimeAndInstitutionalColors()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var document = await new ReadProductReportUseCase(new ProductSampleRepository(), new TestUser(), new FixedClock())
            .ExecuteAsync(new ProductReportFilter(), CancellationToken.None);
        var pdf = new PdfReportGenerator().Generate(document, []);
        using var parsed = UglyToad.PdfPig.PdfDocument.Open(pdf.Content);
        var page = parsed.GetPage(1);
        Assert.True(page.NumberOfImages > 0);
        Assert.Contains("Reporte de productos", page.Text);
        Assert.Contains("Total de productos: 3", page.Text);
        Assert.Contains("Reporte generado: mauricio - 01/11/2026 - 12:00", page.Text);
        Assert.Contains(page.Letters, letter =>
        {
            var (red, green, blue) = letter.Color.ToRGBValues();
            return Math.Abs(red - 30d / 255) < 0.01 && Math.Abs(green - 58d / 255) < 0.01 && Math.Abs(blue - 95d / 255) < 0.01;
        });
        var excel = new ExcelReportGenerator().Generate(document, []);
        using var workbook = new XLWorkbook(new MemoryStream(excel.Content));
        var sheet = workbook.Worksheet(1);
        Assert.Single(sheet.Pictures);
        Assert.Equal("Reporte de productos", sheet.Cell(1, 2).GetString());
        Assert.Equal(XLColor.FromHtml("#1E3A5F"), sheet.Cell(5, 1).Style.Fill.BackgroundColor);
        Assert.Equal(XLColor.FromHtml("#3B694F"), sheet.Cell(11, 1).Style.Font.FontColor);
        Assert.Equal("Total de productos: 3", sheet.Cell(11, 1).GetString());
        Assert.Contains("Reporte generado: mauricio - 01/11/2026 - 12:00", sheet.Cell(12, 1).GetString());
        SaveExample("reporte-productos", pdf.Content, excel.Content);
    }

    [Fact]
    public void TemplateAcceptsDifferentTitlesColumnsValuesSummaryAndOptionalChart()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        // Solo prueba de presentación: no implementa consultas ni funcionalidades de US-101.
        var document = new ReportDocument("Resumen de demostración", [], "Ana", FixedClock.Now)
        {
            Columns = ["Periodo", "Operaciones", "Importe (Bs)"],
            Values = [new object?[] { "Octubre", 2, 150.75m }],
            Summary = ["Total de demostración: 150,75 Bs", "Operaciones: 2"]
        };
        var chart = new Libreria.Servicio.Reportes.Infrastructure.Charts.ScottPlotChartRenderer()
            .Render([new("Muestra", 150.75m)]);
        var pdf = new PdfReportGenerator().Generate(document, chart);
        using var parsed = UglyToad.PdfPig.PdfDocument.Open(pdf.Content);
        var text = string.Concat(parsed.GetPages().Select(page => page.Text));
        foreach (var value in new[] { document.Title, "Periodo", "Octubre", "Operaciones: 2", "Reporte generado: Ana" })
            Assert.Contains(value, text);
        var excel = new ExcelReportGenerator().Generate(document, chart);
        using var workbook = new XLWorkbook(new MemoryStream(excel.Content));
        var sheet = workbook.Worksheet(1);
        Assert.Equal(2, sheet.Pictures.Count);
        Assert.Equal("Operaciones", sheet.Cell(5, 2).GetString());
        Assert.Equal(2, sheet.Cell(6, 2).GetValue<int>());
        Assert.Equal(150.75m, sheet.Cell(6, 3).GetValue<decimal>());
        Assert.Contains(sheet.CellsUsed(), cell => cell.GetString() == "Operaciones: 2");
        Assert.Contains(sheet.CellsUsed(), cell => cell.GetString().Contains("Reporte generado: Ana"));
    }

    [Fact]
    public void LongProductReportRepeatsHeaderAndPlacesSummaryAfterAllRows()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var document = new ReportDocument("Reporte de productos", Enumerable.Range(1, 180)
            .Select(index => new ReportRow($"Producto {index:000}", index, "Papelería", "Escolar")).ToArray(),
            "Usuario de prueba", FixedClock.Now, true) { Summary = ["Total de productos: 180"] };
        var pdf = new PdfReportGenerator().Generate(document, []);
        using var parsed = UglyToad.PdfPig.PdfDocument.Open(pdf.Content);
        Assert.True(parsed.NumberOfPages > 1);
        foreach (var page in parsed.GetPages())
        {
            Assert.Contains("Reporte de productos", page.Text);
            Assert.True(page.NumberOfImages > 0);
            if (page.Text.Contains("Producto ")) Assert.Contains("Categoría", page.Text);
        }
        var lastPage = parsed.GetPage(parsed.NumberOfPages).Text;
        Assert.Contains("Total de productos: 180", lastPage);
        Assert.Contains("Reporte generado: Usuario de prueba", lastPage);
        Assert.Equal(1, parsed.GetPages().Count(page => page.Text.Contains("Total de productos: 180")));
        var excel = new ExcelReportGenerator().Generate(document, []);
        using var workbook = new XLWorkbook(new MemoryStream(excel.Content));
        Assert.Equal("Producto 180", workbook.Worksheet(1).Cell(185, 1).GetString());
        Assert.Equal("Total de productos: 180", workbook.Worksheet(1).Cell(188, 1).GetString());
        SaveExample("reporte-productos-multipagina", pdf.Content, excel.Content);
    }

    private static void SaveExample(string name, byte[] pdf, byte[] excel)
    {
        var output = Environment.GetEnvironmentVariable("LIBRERIA_REPORTES_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, name + ".pdf"), pdf);
        File.WriteAllBytes(Path.Combine(output, name + ".xlsx"), excel);
    }
}
