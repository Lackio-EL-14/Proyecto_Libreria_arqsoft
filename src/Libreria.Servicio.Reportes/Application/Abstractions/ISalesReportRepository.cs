using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.Abstractions;

public interface ISalesReportRepository
{
    Task<IReadOnlyList<SalesReportRow>> ReadSalesAsync(SalesReportFilter filter, CancellationToken cancellationToken);
}
