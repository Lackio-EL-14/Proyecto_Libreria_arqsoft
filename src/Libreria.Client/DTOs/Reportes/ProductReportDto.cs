namespace Libreria.Client.DTOs.Reportes;

// Contratos propios del Front: no referencia tipos del servicio.
public sealed record ProductReportDto(
    string Title, IReadOnlyList<ProductReportRowDto> Rows, string GeneratedBy, DateTimeOffset GeneratedAt);
public sealed record ProductReportRowDto(string Name, decimal Amount, string? Category, string? Brand);
public sealed record ReportCategoryDto(Guid PublicId, string Name);
public sealed record ReportFileDto(byte[] Content, string ContentType, string FileName);
