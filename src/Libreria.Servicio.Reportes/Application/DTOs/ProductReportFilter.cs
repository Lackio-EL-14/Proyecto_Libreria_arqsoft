using System.ComponentModel.DataAnnotations;

namespace Libreria.Servicio.Reportes.Application.DTOs;

public sealed class ProductReportFilter : IValidatableObject
{
    public Guid? CategoryPublicId { get; init; }
    public decimal? MinimumPrice { get; init; }
    public decimal? MaximumPrice { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryPublicId == Guid.Empty)
            yield return new("Seleccione una categoría válida.", [nameof(CategoryPublicId)]);
        if (MinimumPrice < 0)
            yield return new("El precio mínimo no puede ser negativo.", [nameof(MinimumPrice)]);
        if (MaximumPrice < 0)
            yield return new("El precio máximo no puede ser negativo.", [nameof(MaximumPrice)]);
        if (MinimumPrice > MaximumPrice)
            yield return new("El precio mínimo no puede superar al máximo.", [nameof(MaximumPrice)]);
    }
}
