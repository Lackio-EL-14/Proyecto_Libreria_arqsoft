using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Libreria.Web.Pages.Productos.Models;

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
