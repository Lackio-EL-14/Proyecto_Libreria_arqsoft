using System.Globalization;
using ClosedXML.Excel;
using Libreria.Servicio.Reportes.Domain.Reports;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Libreria.Servicio.Reportes.Infrastructure.Export;

// Un solo lugar para la identidad visual y el encabezado/pie de cada formato.
internal static class ReportTemplate
{
    // Colores de Libreria.Client/wwwroot/css/site.css.
    internal const string Primary = "#1E3A5F";
    internal const string Accent = "#3B694F";
    internal const string Background = "#F9F8F5";
    internal const string Border = "#E2DFD8";
    internal static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("es-BO");
    private static readonly Lazy<byte[]> Logo = new(() =>
    {
        using var stream = typeof(ReportTemplate).Assembly.GetManifestResourceStream("ReportLogo.svg")!;
        using var reader = new StreamReader(stream);
        var svg = reader.ReadToEnd();
        // ClosedXML necesita una imagen raster. QuestPDF convierte el SVG existente una sola vez.
        return Document.Create(container => container.Page(page =>
        {
            page.Size(120, 120);
            page.Margin(0);
            page.Content().Svg(svg);
        })).GenerateImages(new ImageGenerationSettings { RasterDpi = 144 }).Single();
    });

    internal static IReadOnlyList<string> GetColumns(ReportDocument document) => document.Columns ??
        (document.IsProductReport ? ["Producto", "Categoría", "Marca", "Precio (Bs)"] : ["Producto", "Precio (Bs)"]);

    internal static IReadOnlyList<IReadOnlyList<object?>> GetValues(ReportDocument document) => document.Values ??
        document.Rows.Select(row => (IReadOnlyList<object?>)(document.IsProductReport
            ? new object?[] { row.Name, row.Category ?? "", row.Brand ?? "", row.Amount }
            : new object?[] { row.Name, row.Amount })).ToArray();

    internal static string GenerationText(ReportDocument document) =>
        $"Reporte generado: {document.GeneratedBy} - {document.GeneratedAt.ToOffset(TimeSpan.FromHours(-4)).ToString("dd/MM/yyyy - HH:mm", Culture)} (UTC-04:00)";

    internal static bool IsNumber(object? value) => value is decimal or double or float or int or long;
    internal static string Format(object? value) => value is decimal amount
        ? amount.ToString("N2", Culture) : Convert.ToString(value, Culture) ?? "";

    internal static void PdfHeader(IContainer container, ReportDocument document)
    {
        container.BorderBottom(2).BorderColor(Accent).PaddingBottom(8).Row(row =>
        {
            row.ConstantItem(52).Image(Logo.Value);
            row.RelativeItem().PaddingLeft(12).AlignMiddle().Column(column =>
            {
                column.Item().Text("LIBRERÍA").FontSize(10).FontColor(Accent).SemiBold();
                column.Item().Text(document.Title).FontSize(18).FontColor(Primary).SemiBold();
            });
        });
    }

    internal static void PdfSummary(IContainer container, ReportDocument document)
    {
        container.BorderTop(1).BorderColor(Accent).Background(Background).Padding(10).Column(column =>
        {
            column.Spacing(4);
            foreach (var line in document.Summary)
                column.Item().Text(line).FontColor(Accent).SemiBold();
            column.Item().Text(GenerationText(document)).FontSize(9).FontColor(Primary);
        });
    }

    internal static void ExcelHeader(IXLWorksheet sheet, ReportDocument document, int columnCount)
    {
        using var image = new MemoryStream(Logo.Value, writable: false);
        sheet.AddPicture(image).MoveTo(sheet.Cell(1, 1)).WithSize(64, 64);
        sheet.Row(1).Height = 54;
        var title = sheet.Range(1, 2, 1, Math.Max(2, columnCount)).Merge();
        title.Value = document.Title;
        title.Style.Font.FontSize = 18;
        title.Style.Font.Bold = true;
        title.Style.Font.FontColor = XLColor.FromHtml(Primary);
        title.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        sheet.Range(1, 1, 1, columnCount).Style.Border.BottomBorder = XLBorderStyleValues.Medium;
        sheet.Range(1, 1, 1, columnCount).Style.Border.BottomBorderColor = XLColor.FromHtml(Accent);
    }

    internal static void ExcelSummary(IXLWorksheet sheet, ReportDocument document, int row, int columnCount)
    {
        foreach (var line in document.Summary)
        {
            var range = sheet.Range(row, 1, row, columnCount).Merge();
            range.Value = line;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml(Background);
            range.Style.Font.FontColor = XLColor.FromHtml(Accent);
            range.Style.Font.Bold = true;
            row++;
        }
        var footer = sheet.Range(row, 1, row, columnCount).Merge();
        footer.Value = GenerationText(document);
        footer.Style.Font.FontColor = XLColor.FromHtml(Primary);
        footer.Style.Alignment.WrapText = true;
        sheet.Row(row).Height = 32;
    }
}
