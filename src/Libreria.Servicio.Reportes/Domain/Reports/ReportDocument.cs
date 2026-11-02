namespace Libreria.Servicio.Reportes.Domain.Reports;

public sealed record ReportDocument(
    string Title, IReadOnlyList<ReportRow> Rows, string GeneratedBy, DateTimeOffset GeneratedAt,
    bool IsProductReport = false);
