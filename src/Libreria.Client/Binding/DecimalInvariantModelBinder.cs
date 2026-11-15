using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Libreria.Client.Binding;

// Enlace de datos de formularios HTML: el navegador envía los decimales con
// punto. Es una preocupación de la interfaz, por eso vive en el Front y no en
// el núcleo de Ventas.
public class DecimalInvariantModelBinder : IModelBinder
{
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

        var value = valueProviderResult.FirstValue;
        var numberStyles = NumberStyles.AllowLeadingSign |
            NumberStyles.AllowDecimalPoint;

        if (decimal.TryParse(
            value,
            numberStyles,
            CultureInfo.InvariantCulture,
            out var decimalValue))
        {
            bindingContext.Result = ModelBindingResult.Success(decimalValue);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            "El valor ingresado no es válido.");

        return Task.CompletedTask;
    }
}
