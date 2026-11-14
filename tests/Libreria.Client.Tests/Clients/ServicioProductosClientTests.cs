using System.Net;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.Productos;
using Libreria.Client.DTOs.Productos;

namespace Libreria.Client.Tests.Clients;

/// <summary>
/// US-113: el cliente de Productos conserva las rutas y la traducción a ResultadoApi
/// que tenía el antiguo VentasApiClient; solo cambia el servicio que responde (vía gateway).
/// </summary>
public sealed class ServicioProductosClientTests
{
    private static readonly Guid PublicId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task La_busqueda_de_categorias_arma_la_consulta_y_lee_la_lista()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, "[]");
        var cliente = new ServicioProductosClient(ManejadorHttpFalso.CrearHttp(manejador));

        var categorias = await cliente.ObtenerCategoriasAsync("  útiles escolares ");

        Assert.Empty(categorias);
        Assert.Equal(HttpMethod.Get, manejador.UltimaPeticion!.Method);
        Assert.Equal(
            "http://localhost:5080/api/categorias?busqueda=%C3%BAtiles%20escolares",
            manejador.UltimaPeticion.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Un_producto_inexistente_devuelve_null()
    {
        var cliente = new ServicioProductosClient(
            ManejadorHttpFalso.CrearHttp(new ManejadorHttpFalso(HttpStatusCode.NotFound)));

        Assert.Null(await cliente.ObtenerProductoAsync(PublicId));
    }

    [Fact]
    public async Task Un_error_en_una_lectura_sigue_lanzando_HttpRequestException()
    {
        var cliente = new ServicioProductosClient(
            ManejadorHttpFalso.CrearHttp(new ManejadorHttpFalso(HttpStatusCode.InternalServerError)));

        await Assert.ThrowsAsync<HttpRequestException>(() => cliente.ObtenerProductosAsync(null, null, null));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task Un_MS_caido_en_una_lectura_lleva_a_servicio_no_disponible(HttpStatusCode estado)
    {
        var cliente = new ServicioProductosClient(
            ManejadorHttpFalso.CrearHttp(new ManejadorHttpFalso(estado)));

        var error = await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => cliente.ObtenerProductoAsync(PublicId));

        Assert.Equal("Productos", error.Servicio);
    }

    [Fact]
    public async Task Sin_conexion_una_lectura_lleva_a_servicio_no_disponible()
    {
        var cliente = new ServicioProductosClient(ManejadorHttpFalso.CrearHttp(new ManejadorSinConexion()));

        await Assert.ThrowsAsync<ServicioNoDisponibleException>(() => cliente.ObtenerMarcasAsync(null));
    }

    [Fact]
    public async Task El_cambio_de_estado_usa_patch_con_el_estado_en_la_consulta()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.NoContent);
        var cliente = new ServicioProductosClient(ManejadorHttpFalso.CrearHttp(manejador));

        var resultado = await cliente.CambiarEstadoMarcaAsync(PublicId, false);

        Assert.True(resultado.Exitoso);
        Assert.Equal(HttpMethod.Patch, manejador.UltimaPeticion!.Method);
        Assert.Equal($"/api/marcas/{PublicId}/estado", manejador.UltimaPeticion.RequestUri!.AbsolutePath);
        Assert.Equal("?nuevoEstado=false", manejador.UltimaPeticion.RequestUri.Query);
    }

    [Fact]
    public async Task Un_400_con_errores_por_campo_llega_al_resultado()
    {
        var cliente = new ServicioProductosClient(ManejadorHttpFalso.CrearHttp(
            new ManejadorHttpFalso(HttpStatusCode.BadRequest, """{"Input.Nombre":"El nombre es obligatorio."}""")));

        var resultado = await cliente.CrearCategoriaAsync(new CategoriaInput());

        Assert.False(resultado.Exitoso);
        Assert.Equal("El nombre es obligatorio.", resultado.Errores["Input.Nombre"]);
    }

    [Fact]
    public async Task Un_400_con_ValidationProblemDetails_llega_con_la_clave_del_formulario()
    {
        const string cuerpo = """
            {"title":"One or more validation errors occurred.","status":400,
             "errors":{"Nombre":["El nombre es obligatorio."],"$.ubicacion":["Seleccione una ubicación."]}}
            """;
        var cliente = new ServicioProductosClient(ManejadorHttpFalso.CrearHttp(
            new ManejadorHttpFalso(HttpStatusCode.BadRequest, cuerpo)));

        var resultado = await cliente.CrearCategoriaAsync(new CategoriaInput());

        Assert.False(resultado.Exitoso);
        Assert.Equal("El nombre es obligatorio.", resultado.Errores["Input.Nombre"]);
        Assert.Equal("Seleccione una ubicación.", resultado.Errores["Input.Ubicacion"]);
    }

    [Fact]
    public async Task Un_409_devuelve_el_mensaje_del_servicio()
    {
        var cliente = new ServicioProductosClient(ManejadorHttpFalso.CrearHttp(
            new ManejadorHttpFalso(HttpStatusCode.Conflict, """{"mensaje":"Ya existe una marca con ese nombre."}""")));

        var resultado = await cliente.CrearMarcaAsync(new MarcaInput());

        Assert.False(resultado.Exitoso);
        Assert.Equal("Ya existe una marca con ese nombre.", resultado.Errores[string.Empty]);
    }

    [Fact]
    public async Task Un_404_sin_mensaje_en_una_escritura_es_NoEncontrado()
    {
        var cliente = new ServicioProductosClient(
            ManejadorHttpFalso.CrearHttp(new ManejadorHttpFalso(HttpStatusCode.NotFound)));

        var resultado = await cliente.DarDeBajaProductoAsync(PublicId);

        Assert.True(resultado.NoEncontrado);
    }

    [Fact]
    public async Task Sin_conexion_una_escritura_no_lanza_y_nombra_al_servicio()
    {
        var cliente = new ServicioProductosClient(ManejadorHttpFalso.CrearHttp(new ManejadorSinConexion()));

        var resultado = await cliente.ActualizarProductoAsync(PublicId, new ProductoInput());

        Assert.False(resultado.Exitoso);
        Assert.Contains("servicio de Productos", resultado.Errores[string.Empty]);
    }
}
