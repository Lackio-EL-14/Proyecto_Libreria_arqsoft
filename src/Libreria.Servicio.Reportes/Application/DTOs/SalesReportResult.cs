using System.Globalization;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.DTOs;

public sealed record SalesReportResult(
    SalesReportFilter Filter, IReadOnlyList<SalesReportRow> Rows, string GeneratedBy, DateTimeOffset GeneratedAt)
{
    public long TotalQuantity => Rows.Sum(row => row.Quantity);
    public decimal TotalAmount => Rows.Sum(row => row.Amount);

    // César puede reutilizar el resultado y este documento sin repetir la consulta.
    public ReportDocument ToDocument() => new("Reporte de ventas", [], GeneratedBy, GeneratedAt)
    {
        Subtitle = $"Desde: {Filter.DateFrom?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "sin límite"} - " +
            $"Hasta: {Filter.DateTo?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "sin límite"}",
        Columns = ["Producto", "Categoría", "Cantidad vendida", "Total vendido (Bs)"],
        Values = Rows.Select(row => (IReadOnlyList<object?>)new object?[]
            { row.Name, row.Category, row.Quantity, row.Amount }).ToArray(),
        Summary = [$"Total de unidades vendidas: {TotalQuantity}",
            $"Total vendido: Bs {TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("es-BO"))}"]
    };
}
