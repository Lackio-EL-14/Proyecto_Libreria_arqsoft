using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.Abstractions;

public interface IExampleReportRepository
{
    Task<IReadOnlyList<ReportRow>> ReadSampleAsync(CancellationToken cancellationToken);
}
