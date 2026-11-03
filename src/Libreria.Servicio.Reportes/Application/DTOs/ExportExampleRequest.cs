using System.ComponentModel.DataAnnotations;

namespace Libreria.Servicio.Reportes.Application.DTOs;

public sealed class ExportExampleRequest
{
    [Required(ErrorMessage = "Debe indicar el formato PDF o Excel.")]
    public ReportFormat? Format { get; init; }
}
