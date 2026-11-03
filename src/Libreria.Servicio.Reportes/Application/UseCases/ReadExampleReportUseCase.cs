using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.UseCases;

public sealed class ReadExampleReportUseCase(
    IExampleReportRepository repository, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<ReportDocument> ExecuteAsync(CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not > 0 || string.IsNullOrWhiteSpace(currentUser.Username))
            throw new UnauthorizedAccessException("Falta el usuario que genera el reporte.");

        var rows = await repository.ReadSampleAsync(cancellationToken);
        return new ReportDocument("Reporte de ejemplo — US-99", rows.ToArray(),
            currentUser.Username, timeProvider.GetUtcNow()) { Summary = [$"Total de registros: {rows.Count}"] };
    }
}
