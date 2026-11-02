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
        var columns = ReportTemplate.GetColumns(document);
        var rows = ReportTemplate.GetValues(document);
        var content = Document.Create(container => container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(style => style.FontSize(10));
            page.Header().Element(header => ReportTemplate.PdfHeader(header, document));
            page.Content().PaddingVertical(15).Column(column =>
            {
                column.Spacing(10);
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(definition =>
                    {
                        foreach (var _ in columns) definition.RelativeColumn();
                    });
                    table.Header(header =>
                    {
                        foreach (var title in columns)
                            header.Cell().Background(ReportTemplate.Primary).Padding(6)
                                .Text(title).FontColor(Colors.White).SemiBold();
                    });
                    foreach (var row in rows)
                    {
                        foreach (var value in row)
                        {
                            var cell = table.Cell().BorderBottom(0.5f).BorderColor(ReportTemplate.Border).Padding(6);
                            if (ReportTemplate.IsNumber(value)) cell = cell.AlignRight();
                            cell.Text(ReportTemplate.Format(value));
                        }
                    }
                });
                if (rows.Count == 0)
                    column.Item().Text(document.IsProductReport
                        ? "No se encontraron productos con los filtros seleccionados."
                        : "No se encontraron registros.");
                if (chartPng.Length > 0) column.Item().Image(chartPng);
                column.Item().ShowEntire().Element(summary => ReportTemplate.PdfSummary(summary, document));
            });
            page.Footer().AlignRight().Text(text =>
            {
                text.Span("Página ");
                text.CurrentPageNumber();
                text.Span(" de ");
                text.TotalPages();
            });
        })).GeneratePdf();
        return new ExportedReport(content, "application/pdf",
            document.IsProductReport ? "reporte-productos.pdf" : "ejemplo-us99.pdf");
    }
}
