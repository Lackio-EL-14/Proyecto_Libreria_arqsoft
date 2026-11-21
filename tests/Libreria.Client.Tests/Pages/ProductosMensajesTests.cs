using Libreria.Client.DTOs.Common;
using Libreria.Client.Pages.Productos;
using Xunit;

namespace Libreria.Client.Tests.Pages;

/// <summary>Auditoría 20/11: una baja rechazada muestra el mensaje del Result, no un 404 vacío.</summary>
public sealed class ProductosMensajesTests
{
    [Fact]
    public void Un_409_muestra_el_mensaje_del_servicio()
    {
        var resultado = ResultadoApi.Invalido("El producto ya está dado de baja.");

        Assert.Equal("El producto ya está dado de baja.", IndexModel.MensajeDeError(resultado, "No se pudo."));
    }

    [Fact]
    public void Un_404_dice_que_el_producto_no_existe()
    {
        Assert.Equal("El producto no existe.", IndexModel.MensajeDeError(ResultadoApi.NoExiste(), "No se pudo."));
    }

    [Fact]
    public void Sin_mensaje_usa_el_texto_por_defecto()
    {
        var resultado = ResultadoApi.Invalido(new Dictionary<string, string>());

        Assert.Equal("No se pudo dar de baja el producto.", IndexModel.MensajeDeError(resultado, "No se pudo dar de baja el producto."));
    }
}
