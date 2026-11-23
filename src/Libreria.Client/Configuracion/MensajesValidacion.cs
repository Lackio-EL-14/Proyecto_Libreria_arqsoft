using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Libreria.Client.Configuracion;

/// <summary>
/// Mensajes de validación en español para todo el sitio (pruebas del 22/11, caso C3).
/// Reemplazan los textos en inglés del enlace de modelos («The value '' is invalid.»,
/// «The field X must be a number.»…). El nombre del campo es su <c>[Display(Name)]</c>.
/// </summary>
public static class MensajesValidacion
{
    public const string CampoObligatorio = "El campo {0} es obligatorio.";

    public static string DebeSerNumero(string campo) => $"El campo {campo} debe ser un número.";

    public static MvcOptions UsarMensajesEnEspanol(this MvcOptions opciones)
    {
        opciones.ModelBindingMessageProvider.UsarMensajesEnEspanol();
        opciones.ModelMetadataDetailsProviders.Add(new RequeridoImplicitoEnEspanol());
        return opciones;
    }

    public static void UsarMensajesEnEspanol(this DefaultModelBindingMessageProvider mensajes)
    {
        mensajes.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor de {campo}.");
        mensajes.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
        mensajes.SetMissingRequestBodyRequiredValueAccessor(() => "Faltan los datos del formulario.");
        mensajes.SetValueMustNotBeNullAccessor(_ => "Este campo es obligatorio.");
        mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor «{valor}» no es válido para {campo}.");
        mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor «{valor}» no es válido.");
        mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor ingresado en {campo} no es válido.");
        mensajes.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor ingresado no es válido.");
        mensajes.SetValueIsInvalidAccessor(valor => $"El valor «{valor}» no es válido.");
        mensajes.SetValueMustBeANumberAccessor(DebeSerNumero);
        mensajes.SetNonPropertyValueMustBeANumberAccessor(() => "Este campo debe ser un número.");
    }
}

/// <summary>
/// MVC genera <c>data-val-required="The X field is required."</c> para las propiedades de tipo
/// valor no anulables que no declaran <c>[Required]</c>. Este proveedor les agrega uno con el
/// mensaje en español (en el servidor nunca falla: un tipo de valor no puede ser nulo).
/// </summary>
public sealed class RequeridoImplicitoEnEspanol : IValidationMetadataProvider
{
    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        var tipo = context.Key.ModelType;
        var esValorNoAnulable = tipo.IsValueType && Nullable.GetUnderlyingType(tipo) is null;

        if (context.Key.MetadataKind != ModelMetadataKind.Property
            || !esValorNoAnulable
            || context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>().Any())
        {
            return;
        }

        context.ValidationMetadata.ValidatorMetadata.Add(
            new RequiredAttribute { ErrorMessage = MensajesValidacion.CampoObligatorio });
    }
}
