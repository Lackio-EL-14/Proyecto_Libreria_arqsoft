using System.Globalization;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace Libreria.Servicio.Reportes.Infrastructure.Export;

public sealed class PdfReportGenerator : IReportGenerator
{
    public ExportedReport Generate(ReportDocument document, byte[] chartPng)
    {
        var content = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontSize(10));
            page.Header().Text(document.Title).FontSize(18).SemiBold();
            page.Content().PaddingVertical(15).Column(column =>
            {
                column.Spacing(10);
                if (!document.IsProductReport)
                    column.Item().Text("Muestra técnica de productos. No es el reporte definitivo.");
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        if (document.IsProductReport)
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        }
                        columns.ConstantColumn(85);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Text("Producto").SemiBold();
                        if (document.IsProductReport)
                        {
                            header.Cell().Text("Categoría").SemiBold();
                            header.Cell().Text("Marca").SemiBold();
                        }
                        header.Cell().AlignRight().Text("Precio (Bs)").SemiBold();
                    });
                    foreach (var row in document.Rows)
                    {
                        table.Cell().PaddingVertical(3).Text(row.Name);
                        if (document.IsProductReport)
                        {
                            table.Cell().PaddingVertical(3).Text(row.Category ?? "");
                            table.Cell().PaddingVertical(3).Text(row.Brand ?? "");
                        }
                        table.Cell().PaddingVertical(3).AlignRight()
                            .Text(row.Amount.ToString("N2", CultureInfo.GetCultureInfo("es-BO")));
                    }
                });
                if (document.Rows.Count == 0) column.Item().Text("No se encontraron productos con los filtros seleccionados.");
                if (chartPng.Length > 0) column.Item().Image(chartPng);
            });
            page.Footer().Text($"Generado por {document.GeneratedBy} · {document.GeneratedAt:yyyy-MM-dd HH:mm} UTC");
        })).GeneratePdf();
        return new ExportedReport(content, "application/pdf",
            document.IsProductReport ? "reporte-productos.pdf" : "ejemplo-us99.pdf");
    }
}
