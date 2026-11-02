namespace Libreria.Servicio.Reportes.Domain.Reports;

public sealed record ReportDocument(
    string Title, IReadOnlyList<ReportRow> Rows, string GeneratedBy, DateTimeOffset GeneratedAt,
    bool IsProductReport = false)
{
    // Contenido opcional para reutilizar los generadores con otras tablas y resúmenes.
    public IReadOnlyList<string>? Columns { get; init; }
    public IReadOnlyList<IReadOnlyList<object?>>? Values { get; init; }
    public IReadOnlyList<string> Summary { get; init; } = [];
    public string? Subtitle { get; init; }
}
