using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.Abstractions;

public interface IReportChartRenderer
{
    byte[] Render(IReadOnlyList<ReportRow> rows);

    byte[] RenderSales(IReadOnlyList<SalesReportRow> rows);
}
