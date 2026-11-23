using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Libreria.Client.Validacion;

/// <summary>
/// Exige que esta propiedad o <see cref="OtraPropiedad"/> tengan valor (por ejemplo, al menos un
/// teléfono o un correo del contacto principal). Valida en el servidor y, con
/// <c>data-val-requeridoalguno</c> (ver <c>wwwroot/js/validacion.js</c>), también en el navegador.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RequeridoAlgunoDeAttribute : ValidationAttribute, IClientModelValidator
{
    public RequeridoAlgunoDeAttribute(string otraPropiedad)
    {
        OtraPropiedad = otraPropiedad;
    }

    public string OtraPropiedad { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var otra = validationContext.ObjectType.GetProperty(OtraPropiedad)
            ?? throw new InvalidOperationException($"No existe la propiedad {OtraPropiedad}.");

        return TieneValor(value) || TieneValor(otra.GetValue(validationContext.ObjectInstance))
            ? ValidationResult.Success
            : new ValidationResult(
                FormatErrorMessage(validationContext.DisplayName),
                validationContext.MemberName is null ? null : [validationContext.MemberName]);
    }

    public void AddValidation(ClientModelValidationContext context)
    {
        Agregar(context.Attributes, "data-val", "true");
        Agregar(context.Attributes, "data-val-requeridoalguno",
            FormatErrorMessage(context.ModelMetadata.GetDisplayName()));
        Agregar(context.Attributes, "data-val-requeridoalguno-otro", "*." + OtraPropiedad);
    }

    private static bool TieneValor(object? valor) => valor is not string texto
        ? valor is not null
        : !string.IsNullOrWhiteSpace(texto);

    private static void Agregar(IDictionary<string, string> atributos, string clave, string valor) =>
        atributos.TryAdd(clave, valor);
}
