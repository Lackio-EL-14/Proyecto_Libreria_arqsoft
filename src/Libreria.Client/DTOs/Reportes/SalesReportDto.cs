namespace Libreria.Client.DTOs.Reportes;

public sealed record SalesReportDto(SalesReportFilterDto Filter, IReadOnlyList<SalesReportRowDto> Rows,
    long TotalQuantity, decimal TotalAmount, string GeneratedBy, DateTimeOffset GeneratedAt);
public sealed record SalesReportRowDto(string Name, string Category, long Quantity, decimal Amount);
