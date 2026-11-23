using System.Globalization;
using System.Net;
using System.Text.Json;
using Libreria.Client.Tests.Infraestructura;

namespace Libreria.Client.Tests.Formularios;

/// <summary>
/// Pruebas del 22/11, caso C3: el formulario de productos no muestra textos en inglés ni nombres
/// internos, valida por campo en español y guarda bien los decimales con cualquier cultura.
/// </summary>
public sealed class ProductosFormularioTests : IClassFixture<FrontFactory>
{
    private const string Categoria = "ca000000-0000-0000-0000-000000000001";
    private const string Marca = "b0000000-0000-0000-0000-000000000001";
    private const string ProductoId = "d0000000-0000-0000-0000-000000000001";

    private static readonly string Catalogos =
        $$"""{"categorias":[{"publicId":"{{Categoria}}","nombre":"Libros"}],"marcas":[{"publicId":"{{Marca}}","nombre":"Norma"}]}""";

    private readonly FrontFactory _front;

    public ProductosFormularioTests(FrontFactory front)
    {
        _front = front;
        _front.Gateway.Responder(HttpMethod.Get, "/api/productos/formulario", HttpStatusCode.OK, Catalogos);
        _front.Gateway.Responder(HttpMethod.Post, "/api/productos", HttpStatusCode.Created, "{}");
        _front.Gateway.Responder(HttpMethod.Get, $"/api/productos/{ProductoId}/edicion", HttpStatusCode.OK,
            $$"""
            {"input":{"nombre":"Cuaderno","descripcionEspecifica":null,"esPerecedero":false,"fechaVencimiento":null,
            "stock":10,"precioVenta":12.5,"costoAdquisicion":8.75,"categoriaPublicId":"{{Categoria}}","marcaPublicId":"{{Marca}}"},
            "categorias":[{"publicId":"{{Categoria}}","nombre":"Libros"}],"marcas":[{"publicId":"{{Marca}}","nombre":"Norma"}]}
            """);
    }

    [Theory]
    [InlineData("/Productos/Create")]
    [InlineData("/Productos/Edit/" + ProductoId)]
    public async Task Las_etiquetas_salen_en_espanol_con_su_marca(string url)
    {
        var html = await _front.LeerPaginaAsync(url);

        Assert.Contains("Nombre <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Descripción específica (opcional)</label>", html);
        Assert.Contains("Categoría <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Marca <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Stock (unidades) <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Precio de venta (Bs.) <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Costo de adquisición (Bs.) <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Es un producto perecedero</label>", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
    }

    [Fact]
    public async Task Los_mensajes_del_cliente_estan_en_espanol()
    {
        var html = await _front.LeerPaginaAsync("/Productos/Create");

        Assert.Contains("data-val-required=\"Seleccione una categoría.\"", html);
        Assert.Contains("data-val-required=\"Seleccione una marca.\"", html);
        Assert.Contains("data-val-required=\"El nombre del producto es obligatorio.\"", html);
        Assert.Contains("data-val-number=\"El campo Precio de venta (Bs.) debe ser un número.\"", html);
        Assert.Contains("data-val-range=\"El stock no puede ser negativo.\"", html);
        Assert.Contains("data-val-range=\"El precio de venta no puede ser negativo.\"", html);
        Assert.Contains("data-val-range=\"El costo de adquisición no puede ser negativo.\"", html);
    }

    [Fact]
    public async Task Un_post_vacio_muestra_un_mensaje_por_campo_sin_llamar_al_servicio()
    {
        var antes = _front.Gateway.Recibidas.Count(r => r.Metodo == "POST");

        var (respuesta, html) = await _front.EnviarFormularioAsync(_front.CrearCliente(), "/Productos/Create",
            new Dictionary<string, string>
            {
                ["Input.Nombre"] = "", ["Input.CategoriaPublicId"] = "", ["Input.MarcaPublicId"] = "",
                ["Input.Stock"] = "", ["Input.PrecioVenta"] = "", ["Input.CostoAdquisicion"] = ""
            });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("El nombre del producto es obligatorio.</span>", html);
        Assert.Contains("Seleccione una categoría.</span>", html);
        Assert.Contains("Seleccione una marca.</span>", html);
        Assert.Contains("Ingrese el stock del producto.</span>", html);
        Assert.Contains("Ingrese el precio de venta.</span>", html);
        Assert.Contains("Ingrese el costo de adquisición.</span>", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
        Assert.Equal(antes, _front.Gateway.Recibidas.Count(r => r.Metodo == "POST"));
    }

    [Fact]
    public async Task Numeros_invalidos_o_negativos_tienen_mensaje_por_campo()
    {
        var (_, html) = await _front.EnviarFormularioAsync(_front.CrearCliente(), "/Productos/Create",
            new Dictionary<string, string>
            {
                ["Input.Nombre"] = "Cuaderno", ["Input.CategoriaPublicId"] = Categoria, ["Input.MarcaPublicId"] = Marca,
                ["Input.Stock"] = "-1", ["Input.PrecioVenta"] = "-5", ["Input.CostoAdquisicion"] = "abc"
            });

        Assert.Contains("El stock no puede ser negativo.</span>", html);
        Assert.Contains("El precio de venta no puede ser negativo.</span>", html);
        Assert.Contains("El campo Costo de adquisición (Bs.) debe ser un número.</span>", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
    }

    [Theory]
    [InlineData("es-BO")]
    [InlineData("en-US")]
    public async Task Un_precio_con_decimales_llega_igual_al_servicio(string cultura)
    {
        var anterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultura);
        try
        {
            var (respuesta, _) = await _front.EnviarFormularioAsync(_front.CrearCliente(), "/Productos/Create",
                new Dictionary<string, string>
                {
                    ["Input.Nombre"] = $"Cuaderno {cultura}", ["Input.CategoriaPublicId"] = Categoria,
                    ["Input.MarcaPublicId"] = Marca, ["Input.Stock"] = "3",
                    ["Input.PrecioVenta"] = "12.50", ["Input.CostoAdquisicion"] = "8.75"
                });

            Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
            var cuerpo = _front.Gateway.Recibidas.Last(r => r.Metodo == "POST" && r.Cuerpo.Contains(cultura)).Cuerpo;
            using var json = JsonDocument.Parse(cuerpo);
            Assert.Equal(12.50m, json.RootElement.GetProperty("precioVenta").GetDecimal());
            Assert.Equal(8.75m, json.RootElement.GetProperty("costoAdquisicion").GetDecimal());
            Assert.Equal(3, json.RootElement.GetProperty("stock").GetInt32());
            Assert.Equal(Categoria, json.RootElement.GetProperty("categoriaPublicId").GetString());
        }
        finally
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = anterior;
        }
    }

    [Fact]
    public async Task La_edicion_muestra_los_decimales_con_punto_en_cultura_espanola()
    {
        var anterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("es-BO");
        try
        {
            var html = await _front.LeerPaginaAsync("/Productos/Edit/" + ProductoId);

            Assert.Contains("value=\"12.5\"", html);
            Assert.Contains("value=\"8.75\"", html);
            Assert.DoesNotContain("value=\"12,5", html);
        }
        finally
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = anterior;
        }
    }
}
