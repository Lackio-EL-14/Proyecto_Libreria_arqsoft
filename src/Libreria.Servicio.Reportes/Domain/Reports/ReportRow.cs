namespace Libreria.Servicio.Reportes.Domain.Reports;

public sealed record ReportRow(string Name, decimal Amount, string? Category = null, string? Brand = null);
