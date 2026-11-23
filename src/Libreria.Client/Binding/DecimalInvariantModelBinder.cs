using System.Globalization;
using Libreria.Client.Configuracion;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Libreria.Client.Binding;

// Enlace de datos de formularios HTML: el navegador envía los decimales con
// punto (input type="number"), sea cual sea la cultura del servidor. Es una
// preocupación de la interfaz, por eso vive en el Front.
// Sirve para decimal y decimal?: un campo vacío queda en null sin error, para
// que el mensaje sea el de [Required] de la propiedad (pruebas del 22/11, C3).
public class DecimalInvariantModelBinder : IModelBinder
{
    private const NumberStyles Estilos = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(
            bindingContext.ModelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(
            bindingContext.ModelName,
            valueProviderResult);

        var value = valueProviderResult.FirstValue?.Trim();

        if (string.IsNullOrEmpty(value))
        {
            if (bindingContext.ModelMetadata.IsReferenceOrNullableType)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        MensajesValidacion.CampoObligatorio,
                        bindingContext.ModelMetadata.GetDisplayName()));
            }

            return Task.CompletedTask;
        }

        if (decimal.TryParse(value, Estilos, CultureInfo.InvariantCulture, out var decimalValue))
        {
            bindingContext.Result = ModelBindingResult.Success(decimalValue);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            MensajesValidacion.DebeSerNumero(bindingContext.ModelMetadata.GetDisplayName()));

        return Task.CompletedTask;
    }
}
