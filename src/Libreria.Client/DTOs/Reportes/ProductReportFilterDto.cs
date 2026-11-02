using System.ComponentModel.DataAnnotations;

namespace Libreria.Client.DTOs.Reportes;

public sealed class ProductReportFilterDto : IValidatableObject
{
    public Guid? CategoryPublicId { get; set; }
    public decimal? MinimumPrice { get; set; }
    public decimal? MaximumPrice { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CategoryPublicId == Guid.Empty)
            yield return new("Seleccione una categoría válida.", [nameof(CategoryPublicId)]);
        if (MinimumPrice < 0 || MaximumPrice < 0)
            yield return new("Los precios no pueden ser negativos.", [nameof(MinimumPrice), nameof(MaximumPrice)]);
        if (MinimumPrice > MaximumPrice)
            yield return new("El precio mínimo no puede superar al máximo.", [nameof(MaximumPrice)]);
    }
}
