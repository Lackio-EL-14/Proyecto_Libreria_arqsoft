using System.Net;
using Libreria.Client.Clients.Clientes;
using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Clientes;

namespace Libreria.Client.Tests.Clients;

/// <summary>US-116: el cliente tipado arma las rutas del contrato de Clientes y traduce sus respuestas.</summary>
public sealed class ServicioClientesClientTests
{
    private static readonly Guid PublicId = Guid.Parse("c0000000-0000-0000-0000-000000000002");

    private const string ClienteJson = """
        {"publicId":"c0000000-0000-0000-0000-000000000002","ciNit":"4567890",
         "razonSocial":"María Fernanda Quispe Choque","correo":"maria.quispe@correo.bo","estado":true,
         "fechaCreacion":"2026-11-14T10:00:00","usuarioCreacionId":"a0000000-0000-0000-0000-000000000001",
         "fechaModificacion":null,"usuarioModificacionId":null}
        """;

    private static (ServicioClientesClient Cliente, ManejadorHttpFalso Manejador) Crear(
        HttpStatusCode estado, string cuerpo = "")
    {
        var manejador = new ManejadorHttpFalso(estado, cuerpo);
        return (new ServicioClientesClient(ManejadorHttpFalso.CrearHttp(manejador)), manejador);
    }

    [Theory]
    [InlineData(EstadoFiltroClientes.Activos, "http://localhost:5080/api/clientes")]
    [InlineData(EstadoFiltroClientes.Inactivos, "http://localhost:5080/api/clientes?estado=false")]
    [InlineData(EstadoFiltroClientes.Todos, "http://localhost:5080/api/clientes?estado=")]
    public async Task El_filtro_de_estado_respeta_el_contrato(EstadoFiltroClientes estado, string urlEsperada)
    {
        var (cliente, manejador) = Crear(HttpStatusCode.OK, "[]");

        await cliente.ObtenerClientesAsync(new FiltroClientes(Estado: estado));

        Assert.Equal(HttpMethod.Get, manejador.UltimaPeticion!.Method);
        Assert.Equal(urlEsperada, manejador.UltimaPeticion.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task La_busqueda_y_el_orden_viajan_en_la_consulta()
    {
        var (cliente, manejador) = Crear(HttpStatusCode.OK, $"[{ClienteJson}]");

        var clientes = await cliente.ObtenerClientesAsync(
            new FiltroClientes("  María Quispe ", EstadoFiltroClientes.Todos, OrdenClientes.FechaCreacion, true));

        Assert.Equal(
            "http://localhost:5080/api/clientes?busqueda=Mar%C3%ADa%20Quispe&orden=FechaCreacion&descendente=true&estado=",
            manejador.UltimaPeticion!.RequestUri!.AbsoluteUri);
        var leido = Assert.Single(clientes);
        Assert.Equal(PublicId, leido.PublicId);
        Assert.Equal(Guid.Parse("a0000000-0000-0000-0000-000000000001"), leido.UsuarioCreacionId);
    }

    [Fact]
    public async Task Un_cliente_inexistente_devuelve_null()
    {
        var (cliente, manejador) = Crear(HttpStatusCode.NotFound);

        Assert.Null(await cliente.ObtenerClienteAsync(PublicId));
        Assert.Equal($"http://localhost:5080/api/clientes/{PublicId}", manejador.UltimaPeticion!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Con_el_MS_caido_la_lectura_lleva_a_servicio_no_disponible()
    {
        var (cliente, _) = Crear(HttpStatusCode.ServiceUnavailable);

        var error = await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => cliente.ObtenerClientesAsync(new FiltroClientes()));
        Assert.Equal("Clientes", error.Servicio);
    }

    [Fact]
    public async Task El_alta_envia_el_cuerpo_y_un_201_es_correcto()
    {
        var (cliente, manejador) = Crear(HttpStatusCode.Created, "{}");

        var resultado = await cliente.RegistrarClienteAsync(
            new ClienteInput { CiNit = "7654321", RazonSocial = "Papelera Andina", Correo = null });

        Assert.True(resultado.Exitoso);
        Assert.Equal(HttpMethod.Post, manejador.UltimaPeticion!.Method);
        Assert.Equal("http://localhost:5080/api/clientes", manejador.UltimaPeticion.RequestUri!.AbsoluteUri);
        Assert.Contains("\"ciNit\":\"7654321\"", manejador.UltimoCuerpo);
        Assert.Contains("\"razonSocial\":\"Papelera Andina\"", manejador.UltimoCuerpo);
    }

    [Fact]
    public async Task Un_400_del_MS_llega_como_error_del_campo_del_formulario()
    {
        var (cliente, _) = Crear(HttpStatusCode.BadRequest,
            """{"title":"Uno o más datos no son válidos.","status":400,"errors":{"Correo":["Ingrese un correo electrónico válido."]}}""");

        var resultado = await cliente.RegistrarClienteAsync(new ClienteInput { CiNit = "1", RazonSocial = "X", Correo = "malo" });

        Assert.False(resultado.Exitoso);
        Assert.Equal("Ingrese un correo electrónico válido.", resultado.Errores["Input.Correo"]);
    }

    [Fact]
    public async Task Un_409_por_CI_NIT_duplicado_llega_como_mensaje()
    {
        var (cliente, manejador) = Crear(HttpStatusCode.Conflict,
            """{"status":409,"detail":"Ya existe un cliente registrado con ese CI/NIT.","errors":{"CiNit":["Ya existe un cliente registrado con ese CI/NIT."]}}""");

        var resultado = await cliente.ActualizarClienteAsync(
            PublicId, new ClienteInput { CiNit = "9999999", RazonSocial = "María" });

        Assert.False(resultado.Exitoso);
        Assert.Equal(HttpMethod.Put, manejador.UltimaPeticion!.Method);
        Assert.Equal($"http://localhost:5080/api/clientes/{PublicId}", manejador.UltimaPeticion.RequestUri!.AbsoluteUri);
        Assert.Contains("Ya existe un cliente registrado con ese CI/NIT.", resultado.Errores.Values);
    }

    [Theory]
    [InlineData(false, """{"activo":false}""")]
    [InlineData(true, """{"activo":true}""")]
    public async Task El_cambio_de_estado_envia_PATCH_con_el_cuerpo_del_contrato(bool activo, string cuerpoEsperado)
    {
        var (cliente, manejador) = Crear(HttpStatusCode.NoContent);

        var resultado = await cliente.CambiarEstadoClienteAsync(PublicId, activo);

        Assert.True(resultado.Exitoso);
        Assert.Equal(HttpMethod.Patch, manejador.UltimaPeticion!.Method);
        Assert.Equal($"http://localhost:5080/api/clientes/{PublicId}/estado", manejador.UltimaPeticion.RequestUri!.AbsoluteUri);
        Assert.Equal(cuerpoEsperado, manejador.UltimoCuerpo);
    }

    [Fact]
    public async Task Sin_conexion_la_escritura_no_lanza()
    {
        var cliente = new ServicioClientesClient(ManejadorHttpFalso.CrearHttp(new ManejadorSinConexion()));

        var resultado = await cliente.CambiarEstadoClienteAsync(PublicId, false);

        Assert.False(resultado.Exitoso);
        Assert.Contains("No se pudo conectar con el servicio de Clientes", resultado.Errores[string.Empty]);
    }
}
