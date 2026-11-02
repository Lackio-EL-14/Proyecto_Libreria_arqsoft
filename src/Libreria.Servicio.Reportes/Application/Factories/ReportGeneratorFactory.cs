using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;

namespace Libreria.Servicio.Reportes.Application.Factories;

// Creator: cada subclase decide qué producto concreto construye.
public abstract class ReportGeneratorFactory
{
    public abstract ReportFormat Format { get; }
    public abstract IReportGenerator CreateGenerator();
}
