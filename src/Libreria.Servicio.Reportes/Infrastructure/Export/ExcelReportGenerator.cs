using ClosedXML.Excel;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Infrastructure.Export;

public sealed class ExcelReportGenerator : IReportGenerator
{
    public ExportedReport Generate(ReportDocument document, byte[] chartPng)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(document.IsProductReport ? "Productos" : "Ejemplo US-99");
        sheet.Cell(1, 1).Value = document.Title;
        sheet.Cell(2, 1).Value = "Generado por";
        sheet.Cell(2, 2).Value = document.GeneratedBy;
        sheet.Cell(3, 1).Value = "Fecha UTC";
        sheet.Cell(3, 2).Value = document.GeneratedAt.UtcDateTime;
        sheet.Cell(5, 1).Value = "Producto";
        var priceColumn = document.IsProductReport ? 4 : 2;
        if (document.IsProductReport)
        {
            sheet.Cell(5, 2).Value = "Categoría";
            sheet.Cell(5, 3).Value = "Marca";
            sheet.Column(3).Width = 24;
        }
        sheet.Cell(5, priceColumn).Value = "Precio (Bs)";
        sheet.Range(5, 1, 5, priceColumn).Style.Font.Bold = true;
        var rowNumber = 6;
        foreach (var row in document.Rows)
        {
            // Value almacena texto incluso si comienza por '='; nunca se asigna FormulaA1.
            sheet.Cell(rowNumber, 1).Value = row.Name;
            if (document.IsProductReport)
            {
                sheet.Cell(rowNumber, 2).Value = row.Category ?? "";
                sheet.Cell(rowNumber, 3).Value = row.Brand ?? "";
            }
            sheet.Cell(rowNumber++, priceColumn).Value = row.Amount;
        }
        if (document.Rows.Count == 0)
            sheet.Cell(rowNumber++, 1).Value = "No se encontraron productos con los filtros seleccionados.";
        sheet.Column(1).Width = 45;
        sheet.Column(2).Width = 24;
        sheet.Column(priceColumn).Width = 24;
        sheet.Range(6, priceColumn, Math.Max(6, rowNumber - 1), priceColumn).Style.NumberFormat.Format = "#,##0.00";
        sheet.Cell(3, 2).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
        if (chartPng.Length > 0)
        {
            using var image = new MemoryStream(chartPng, writable: false);
            sheet.AddPicture(image).MoveTo(sheet.Cell(rowNumber + 2, 1)).WithSize(720, 360);
        }
        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return new ExportedReport(output.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            document.IsProductReport ? "reporte-productos.xlsx" : "ejemplo-us99.xlsx");
    }
}
