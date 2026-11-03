using System.ComponentModel.DataAnnotations;

namespace Libreria.Client.DTOs.Reportes;

public sealed class SalesReportFilterDto : IValidatableObject
{
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public Guid? CategoryPublicId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom > DateTo)
            yield return new("La fecha desde no puede ser posterior a la fecha hasta.", [nameof(DateTo)]);
        if (CategoryPublicId == Guid.Empty)
            yield return new("Seleccione una categoría válida.", [nameof(CategoryPublicId)]);
    }
}
