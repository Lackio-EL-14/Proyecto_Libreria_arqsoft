using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.Abstractions;

public interface IProductReportRepository
{
    Task<IReadOnlyList<ReportCategory>> ReadCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportRow>> ReadProductsAsync(ProductReportFilter filter, CancellationToken cancellationToken);
}
