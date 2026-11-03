using System.ComponentModel.DataAnnotations;

namespace Libreria.Servicio.Reportes.Application.DTOs;

public sealed class SalesReportFilter : IValidatableObject
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public Guid? CategoryPublicId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom > DateTo)
            yield return new("La fecha desde no puede ser posterior a la fecha hasta.", [nameof(DateTo)]);
        if (CategoryPublicId == Guid.Empty)
            yield return new("Seleccione una categoría válida.", [nameof(CategoryPublicId)]);
    }
}
