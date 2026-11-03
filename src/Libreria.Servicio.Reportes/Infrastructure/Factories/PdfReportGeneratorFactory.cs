using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Factories;
using Libreria.Servicio.Reportes.Infrastructure.Export;

namespace Libreria.Servicio.Reportes.Infrastructure.Factories;

public sealed class PdfReportGeneratorFactory : ReportGeneratorFactory
{
    public override ReportFormat Format => ReportFormat.Pdf;
    public override IReportGenerator CreateGenerator() => new PdfReportGenerator();
}
