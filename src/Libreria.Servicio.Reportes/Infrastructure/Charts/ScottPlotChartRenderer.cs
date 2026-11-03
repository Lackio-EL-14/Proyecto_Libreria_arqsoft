using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Infrastructure.Charts;

// Prueba local de viabilidad. El gráfico definitivo de US-101 se implementa después.
public sealed class ScottPlotChartRenderer : IReportChartRenderer
{
    public byte[] Render(IReadOnlyList<ReportRow> rows)
    {
        using var plot = new ScottPlot.Plot();
        plot.Title("Muestra de precios — US-99");
        plot.YLabel("Precio (Bs)");
        if (rows.Count > 0)
        {
            plot.Add.Bars(rows.Select(row => (double)row.Amount).ToArray());
            plot.Axes.Bottom.SetTicks(
                Enumerable.Range(0, rows.Count).Select(index => (double)index).ToArray(),
                rows.Select(row => row.Name.Length > 18 ? row.Name[..18] + "…" : row.Name).ToArray());
        }
        else { plot.Title("Sin datos para el ejemplo — US-99"); }
        return plot.GetImage(720, 360).GetImageBytes();
    }
}
