using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Infrastructure.Charts;

public sealed class ScottPlotChartRenderer : IReportChartRenderer
{
    public byte[] Render(IReadOnlyList<ReportRow> rows) =>
        RenderBars(
            rows.Select(row => row.Name).ToArray(),
            rows.Select(row => (double)row.Amount).ToArray(),
            "Valores del reporte",
            "Importe (Bs)");

    public byte[] RenderSales(IReadOnlyList<SalesReportRow> rows) =>
        RenderBars(
            rows.Select(row => row.Name).ToArray(),
            rows.Select(row => (double)row.Amount).ToArray(),
            "Ventas por producto",
            "Total vendido (Bs)");

    private static byte[] RenderBars(
        IReadOnlyList<string> labels,
        IReadOnlyList<double> values,
        string title,
        string yLabel)
    {
        using var plot = new ScottPlot.Plot();
        plot.Title(title);
        plot.YLabel(yLabel);

        if (values.Count > 0)
        {
            plot.Add.Bars(values.ToArray());
            plot.Axes.Bottom.SetTicks(
                Enumerable.Range(0, labels.Count)
                    .Select(index => (double)index)
                    .ToArray(),
                labels
                    .Select(label => label.Length > 18
                        ? label[..18] + "…"
                        : label)
                    .ToArray());
        }

        return plot.GetImage(720, 360).GetImageBytes();
    }
}
