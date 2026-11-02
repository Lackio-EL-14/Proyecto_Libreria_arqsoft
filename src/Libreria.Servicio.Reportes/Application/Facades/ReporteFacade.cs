using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Factories;
using Libreria.Servicio.Reportes.Application.UseCases;

namespace Libreria.Servicio.Reportes.Application.Facades;

public sealed class ReporteFacade(
    ReadExampleReportUseCase readExample, IReportChartRenderer chartRenderer,
    IEnumerable<ReportGeneratorFactory> factories, ReadProductReportUseCase readProducts)
{
    public async Task<ExportedReport> ExportExampleAsync(
        ReportFormat format, CancellationToken cancellationToken)
    {
        var factory = FindFactory(format);

        var document = await readExample.ExecuteAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var chartPng = chartRenderer.Render(document.Rows);
        return factory.CreateGenerator().Generate(document, chartPng);
    }

    public async Task<ExportedReport> ExportProductsAsync(
        ProductReportFilter filter, ReportFormat format, CancellationToken cancellationToken)
    {
        var factory = FindFactory(format);
        var document = await readProducts.ExecuteAsync(filter, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        return factory.CreateGenerator().Generate(document, []);
    }

    private ReportGeneratorFactory FindFactory(ReportFormat format) =>
        factories.FirstOrDefault(candidate => candidate.Format == format)
        ?? throw new ArgumentOutOfRangeException(nameof(format), "Formato no disponible.");
}
