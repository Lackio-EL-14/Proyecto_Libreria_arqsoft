using System.Globalization;
using Libreria.Client.Binding;
using Libreria.Client.Configuracion;
using Libreria.Client.DTOs.Productos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Libreria.Client.Tests.Configuracion;

/// <summary>Pruebas del 22/11 (C3): los mensajes de enlace de modelos de todo el sitio están en español.</summary>
public sealed class MensajesValidacionTests
{
    private static readonly string[] Ingles = ["value", "field", "invalid", "must", "required", "provided", "number"];

    [Fact]
    public void Todos_los_mensajes_de_enlace_estan_en_espanol()
    {
        var mensajes = new MvcOptions().UsarMensajesEnEspanol().ModelBindingMessageProvider;

        var textos = new[]
        {
            mensajes.MissingBindRequiredValueAccessor("Stock"),
            mensajes.MissingKeyOrValueAccessor(),
            mensajes.MissingRequestBodyRequiredValueAccessor(),
            mensajes.ValueMustNotBeNullAccessor(""),
            mensajes.AttemptedValueIsInvalidAccessor("abc", "Stock"),
            mensajes.NonPropertyAttemptedValueIsInvalidAccessor("abc"),
            mensajes.UnknownValueIsInvalidAccessor("Stock"),
            mensajes.NonPropertyUnknownValueIsInvalidAccessor(),
            mensajes.ValueIsInvalidAccessor("abc"),
            mensajes.ValueMustBeANumberAccessor("Stock"),
            mensajes.NonPropertyValueMustBeANumberAccessor()
        };

        Assert.All(textos, texto =>
        {
            Assert.False(string.IsNullOrWhiteSpace(texto));
            Assert.DoesNotContain(Ingles, palabra => texto.Contains(palabra, StringComparison.OrdinalIgnoreCase));
            Assert.EndsWith(".", texto);
        });
        Assert.Equal("El campo Stock debe ser un número.", mensajes.ValueMustBeANumberAccessor("Stock"));
        Assert.Equal("El valor «abc» no es válido para Stock.", mensajes.AttemptedValueIsInvalidAccessor("abc", "Stock"));
    }

    [Theory]
    [InlineData("12.50", 12.50)]
    [InlineData("0", 0)]
    [InlineData(" 8.75 ", 8.75)]
    public async Task El_decimal_se_lee_con_punto_en_cualquier_cultura(string valor, double esperado)
    {
        var anterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-BO");
        try
        {
            var contexto = await EnlazarAsync(nameof(ProductoInput.PrecioVenta), valor);

            Assert.True(contexto.Result.IsModelSet);
            Assert.Equal((decimal)esperado, contexto.Result.Model);
            Assert.Equal(0, contexto.ModelState.ErrorCount);
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }

    [Fact]
    public async Task Un_decimal_vacio_queda_nulo_para_que_hable_el_Required()
    {
        var contexto = await EnlazarAsync(nameof(ProductoInput.PrecioVenta), "");

        Assert.True(contexto.Result.IsModelSet);
        Assert.Null(contexto.Result.Model);
        Assert.Equal(0, contexto.ModelState.ErrorCount);
    }

    [Fact]
    public async Task Un_decimal_invalido_dice_que_debe_ser_un_numero()
    {
        var contexto = await EnlazarAsync(nameof(ProductoInput.CostoAdquisicion), "abc");

        Assert.False(contexto.Result.IsModelSet);
        var error = Assert.Single(contexto.ModelState[nameof(ProductoInput.CostoAdquisicion)]!.Errors);
        Assert.Equal("El campo Costo de adquisición (Bs.) debe ser un número.", error.ErrorMessage);
    }

    private static async Task<DefaultModelBindingContext> EnlazarAsync(string propiedad, string valor)
    {
        var metadata = MetadataConDataAnnotations().GetMetadataForProperty(typeof(ProductoInput), propiedad);
        var contexto = new DefaultModelBindingContext
        {
            ModelMetadata = metadata,
            ModelName = propiedad,
            ModelState = new ModelStateDictionary(),
            ValueProvider = new QueryStringValueProvider(
                BindingSource.Form,
                new Microsoft.AspNetCore.Http.QueryCollection(
                    new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { [propiedad] = valor }),
                CultureInfo.InvariantCulture)
        };

        await new DecimalInvariantModelBinder().BindModelAsync(contexto);
        return contexto;
    }

    // El mismo proveedor de metadatos que usa MVC, para que el binder vea el [Display(Name)].
    private static IModelMetadataProvider MetadataConDataAnnotations() =>
        ServiciosMvc.Value.GetRequiredService<IModelMetadataProvider>();

    private static readonly Lazy<ServiceProvider> ServiciosMvc = new(() =>
    {
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddMvcCore().AddDataAnnotations();
        return servicios.BuildServiceProvider();
    });
}
