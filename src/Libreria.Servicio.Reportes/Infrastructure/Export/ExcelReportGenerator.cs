using ClosedXML.Excel;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Infrastructure.Export;

public sealed class ExcelReportGenerator : IReportGenerator
{
    public ExportedReport Generate(ReportDocument document, byte[] chartPng)
    {
        var columns = ReportTemplate.GetColumns(document);
        var rows = ReportTemplate.GetValues(document);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(document.IsProductReport ? "Productos" : "Reporte");
        sheet.Style.Font.FontName = "Calibri";
        sheet.Style.Font.FontSize = 11;
        ReportTemplate.ExcelHeader(sheet, document, columns.Count);
        for (var index = 0; index < columns.Count; index++)
            sheet.Cell(5, index + 1).Value = columns[index];
        var header = sheet.Range(5, 1, 5, columns.Count);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(ReportTemplate.Primary);
        sheet.Row(5).Height = 24;
        var rowNumber = 6;
        foreach (var row in rows)
        {
            for (var index = 0; index < row.Count; index++)
            {
                var cell = sheet.Cell(rowNumber, index + 1);
                // FromObject conserva los números y trata '=...' como texto, nunca como fórmula.
                cell.Value = XLCellValue.FromObject(row[index], ReportTemplate.Culture);
                if (ReportTemplate.IsNumber(row[index])) cell.Style.NumberFormat.Format = "#,##0.00";
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                cell.Style.Border.BottomBorderColor = XLColor.FromHtml(ReportTemplate.Border);
            }
            rowNumber++;
        }
        if (rows.Count == 0)
        {
            sheet.Range(rowNumber, 1, rowNumber, columns.Count).Merge().Value = document.IsProductReport
                ? "No se encontraron productos con los filtros seleccionados."
                : "No se encontraron registros.";
            rowNumber++;
        }
        for (var index = 1; index <= columns.Count; index++)
        {
            sheet.Column(index).AdjustToContents(5, Math.Max(6, rowNumber - 1));
            sheet.Column(index).Width = Math.Clamp(sheet.Column(index).Width + 2, 20, 45);
        }
        if (chartPng.Length > 0)
        {
            using var image = new MemoryStream(chartPng, writable: false);
            sheet.AddPicture(image).MoveTo(sheet.Cell(rowNumber + 1, 1)).WithSize(720, 360);
            rowNumber += 20;
        }
        ReportTemplate.ExcelSummary(sheet, document, rowNumber + 2, columns.Count);
        sheet.SheetView.FreezeRows(5);
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return new ExportedReport(output.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            document.IsProductReport ? "reporte-productos.xlsx" : "ejemplo-us99.xlsx");
    }
}
