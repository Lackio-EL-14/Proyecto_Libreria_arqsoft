using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Application.Abstractions;

public interface IReportGenerator
{
    ExportedReport Generate(ReportDocument document, byte[] chartPng);
}
