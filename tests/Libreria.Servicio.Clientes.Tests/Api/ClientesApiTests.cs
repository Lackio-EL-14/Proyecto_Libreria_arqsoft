using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Libreria.Servicio.Clientes.Tests.Fakes;

namespace Libreria.Servicio.Clientes.Tests.Api;

/// <summary>
/// Contrato HTTP de US-115 (sección 6.6) sobre el pipeline real de ASP.NET:
/// rutas, enlace de parámetros, códigos de estado y forma del JSON.
/// </summary>
public sealed class ClientesApiTests : IDisposable
{
    private readonly ClientesApiFactory _fabrica = ClientesApiFactory.ConRepositorioEnMemoria();
    private readonly HttpClient _http;

    public ClientesApiTests()
    {
        _http = _fabrica.CreateClient();
    }

    private ClienteRepositoryFalso Repositorio => _fabrica.Repositorio;

    public void Dispose()
    {
        _http.Dispose();
        _fabrica.Dispose();
    }

    // ---------- POST api/clientes (Insert) ----------

    [Fact]
    public async Task Alta_valida_devuelve_201_con_Location_y_la_forma_de_ClienteRegistroResult()
    {
        var respuesta = await _http.PostAsJsonAsync("api/clientes",
            new { ciNit = " 7654321 ", razonSocial = "Ana  Rojas", correo = "ana@correo.bo" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var json = await Json(respuesta);
        Assert.True(json.GetProperty("exitoso").GetBoolean());
        Assert.Empty(json.GetProperty("errores").EnumerateObject());
        var cliente = json.GetProperty("cliente");
        Assert.Equal("7654321", cliente.GetProperty("ciNit").GetString());
        Assert.Equal("Ana Rojas", cliente.GetProperty("razonSocial").GetString());
        var publicId = cliente.GetProperty("publicId").GetGuid();

        Assert.EndsWith($"/api/clientes/{publicId}", respuesta.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        var creado = Assert.Single(Repositorio.Clientes);
        Assert.Equal(ClienteRepositoryFalso.UsuarioPrueba, creado.UsuarioCreacionId);
        Assert.True(creado.Estado);
    }

    [Fact]
    public async Task Detras_del_gateway_el_Location_apunta_al_gateway()
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "api/clientes")
        {
            Content = JsonContent.Create(new { ciNit = "7654321", razonSocial = "Ana Rojas" })
        };
        peticion.Headers.Add("X-Forwarded-Host", "localhost:5080");
        peticion.Headers.Add("X-Forwarded-Proto", "http");

        var respuesta = await _http.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.StartsWith("http://localhost:5080/api/clientes/", respuesta.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Alta_sin_datos_obligatorios_devuelve_400_con_errores_por_campo()
    {
        var respuesta = await _http.PostAsJsonAsync("api/clientes",
            new { ciNit = "", razonSocial = "  ", correo = "no-es-correo" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await Json(respuesta)).GetProperty("errors");
        Assert.True(errores.TryGetProperty("CiNit", out _));
        Assert.True(errores.TryGetProperty("RazonSocial", out _));
        Assert.True(errores.TryGetProperty("Correo", out _));
        Assert.Empty(Repositorio.Clientes);
    }

    [Fact]
    public async Task Alta_con_CiNit_repetido_devuelve_409_con_el_error_en_CiNit()
    {
        Repositorio.Agregar("9999999", "Universidad Mayor");

        var respuesta = await _http.PostAsJsonAsync("api/clientes",
            new { ciNit = "9999999", razonSocial = "Otra", correo = (string?)null });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var json = await Json(respuesta);
        Assert.Equal("Ya existe un cliente registrado con ese CI/NIT.", json.GetProperty("detail").GetString());
        Assert.True(json.GetProperty("errors").TryGetProperty("CiNit", out _));
    }

    [Fact]
    public async Task Alta_sin_cuerpo_devuelve_400()
    {
        var respuesta = await _http.PostAsync("api/clientes",
            new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // ---------- GET api/clientes, GET api/clientes/{publicId} (Select) ----------

    [Fact]
    public async Task Listado_sin_estado_devuelve_solo_activos()
    {
        Repositorio.Agregar("9999999", "Universidad Mayor");
        Repositorio.Agregar("1234567", "Juan Pérez", estado: false);

        var clientes = await _http.GetFromJsonAsync<JsonElement>("api/clientes");

        Assert.Equal(["Universidad Mayor"], Nombres(clientes));
    }

    [Theory]
    [InlineData("api/clientes?estado=false", new[] { "Juan Pérez" })]
    [InlineData("api/clientes?estado=", new[] { "Juan Pérez", "Universidad Mayor" })]
    [InlineData("api/clientes?estado=&busqueda=Mayor", new[] { "Universidad Mayor" })]
    public async Task Listado_filtra_por_estado_y_busqueda(string ruta, string[] esperados)
    {
        Repositorio.Agregar("9999999", "Universidad Mayor");
        Repositorio.Agregar("1234567", "Juan Pérez", estado: false);

        var clientes = await _http.GetFromJsonAsync<JsonElement>(ruta);

        Assert.Equal(esperados, Nombres(clientes));
    }

    [Fact]
    public async Task Listado_con_orden_desconocido_devuelve_400()
    {
        var respuesta = await _http.GetAsync("api/clientes?orden=ClienteId");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Listado_no_expone_el_id_numerico()
    {
        Repositorio.Agregar("9999999", "Universidad Mayor");

        var cliente = (await _http.GetFromJsonAsync<JsonElement>("api/clientes"))[0];

        Assert.False(cliente.TryGetProperty("clienteId", out _));
        Assert.True(cliente.TryGetProperty("publicId", out _));
        Assert.True(cliente.TryGetProperty("usuarioCreacionId", out _));
    }

    [Fact]
    public async Task Obtener_existente_devuelve_200_y_desconocido_404()
    {
        var cliente = Repositorio.Agregar("9999999", "Universidad Mayor", correo: "compras@um.edu.bo");

        var encontrado = await _http.GetFromJsonAsync<JsonElement>($"api/clientes/{cliente.PublicId}");
        var noExiste = await _http.GetAsync($"api/clientes/{Guid.NewGuid()}");

        Assert.Equal("compras@um.edu.bo", encontrado.GetProperty("correo").GetString());
        Assert.Equal(HttpStatusCode.NotFound, noExiste.StatusCode);
        Assert.Equal("El cliente no existe.", (await Json(noExiste)).GetProperty("detail").GetString());
    }

    // ---------- GET api/clientes/buscar (venta) ----------

    [Fact]
    public async Task Buscar_para_la_venta_no_devuelve_inactivos()
    {
        var activo = Repositorio.Agregar("1234500", "Activo");
        Repositorio.Agregar("1234567", "Inactivo", estado: false);

        var clientes = await _http.GetFromJsonAsync<JsonElement>("api/clientes/buscar?ciNit=12345");

        var unico = Assert.Single(clientes.EnumerateArray());
        Assert.Equal(activo.PublicId, unico.GetProperty("publicId").GetGuid());
        Assert.Equal("1234500", unico.GetProperty("ciNit").GetString());
        Assert.Equal("Activo", unico.GetProperty("razonSocial").GetString());
    }

    [Fact]
    public async Task Buscar_sin_CiNit_devuelve_400()
    {
        var respuesta = await _http.GetAsync("api/clientes/buscar?ciNit=");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    // ---------- PUT api/clientes/{publicId} (Update) ----------

    [Fact]
    public async Task Editar_valido_devuelve_200_y_registra_quien_modifico()
    {
        var cliente = Repositorio.Agregar("9999999", "Universidad Mayor");

        var respuesta = await _http.PutAsJsonAsync($"api/clientes/{cliente.PublicId}",
            new { ciNit = "9999998", razonSocial = "Universidad Mayor de San Andrés", correo = "um@um.edu.bo" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var json = await Json(respuesta);
        Assert.Equal("9999998", json.GetProperty("ciNit").GetString());
        Assert.Equal(ClienteRepositoryFalso.UsuarioPrueba, json.GetProperty("usuarioModificacionId").GetGuid());
    }

    [Fact]
    public async Task Editar_con_el_CiNit_de_otro_cliente_devuelve_409()
    {
        Repositorio.Agregar("9999999", "Universidad Mayor");
        var otro = Repositorio.Agregar("4567890", "María Quispe");

        var respuesta = await _http.PutAsJsonAsync($"api/clientes/{otro.PublicId}",
            new { ciNit = "9999999", razonSocial = "María Quispe" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("4567890", Repositorio.Clientes.Single(c => c.PublicId == otro.PublicId).CiNit);
    }

    [Fact]
    public async Task Editar_conservando_su_propio_CiNit_es_valido()
    {
        var cliente = Repositorio.Agregar("9999999", "Universidad Mayor");

        var respuesta = await _http.PutAsJsonAsync($"api/clientes/{cliente.PublicId}",
            new { ciNit = "9999999", razonSocial = "Universidad Mayor (central)" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.BadRequest)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task Editar_invalido_o_inexistente(bool inexistente, HttpStatusCode esperado)
    {
        var id = inexistente ? Guid.NewGuid() : Repositorio.Agregar("9999999", "Universidad Mayor").PublicId;
        object cuerpo = inexistente
            ? new { ciNit = "1", razonSocial = "X" }
            : new { ciNit = "", razonSocial = "" };

        var respuesta = await _http.PutAsJsonAsync($"api/clientes/{id}", cuerpo);

        Assert.Equal(esperado, respuesta.StatusCode);
    }

    [Fact]
    public async Task Editar_un_cliente_inactivo_devuelve_409()
    {
        var cliente = Repositorio.Agregar("1234567", "Juan Pérez", estado: false);

        var respuesta = await _http.PutAsJsonAsync($"api/clientes/{cliente.PublicId}",
            new { ciNit = "1234567", razonSocial = "Juan Pérez Rojas" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
    }

    // ---------- PATCH api/clientes/{publicId}/estado (Delete lógico) ----------

    [Fact]
    public async Task Baja_logica_conserva_la_fila_lo_saca_de_buscar_y_se_puede_reactivar()
    {
        var cliente = Repositorio.Agregar("9999999", "Universidad Mayor");

        var baja = await _http.PatchAsJsonAsync($"api/clientes/{cliente.PublicId}/estado", new { activo = false });
        var enBuscar = await _http.GetFromJsonAsync<JsonElement>("api/clientes/buscar?ciNit=9999999");
        var detalle = await _http.GetFromJsonAsync<JsonElement>($"api/clientes/{cliente.PublicId}");

        Assert.Equal(HttpStatusCode.NoContent, baja.StatusCode);
        Assert.Empty(enBuscar.EnumerateArray());
        Assert.False(detalle.GetProperty("estado").GetBoolean());
        Assert.Single(Repositorio.Clientes);

        var reactivar = await _http.PatchAsJsonAsync($"api/clientes/{cliente.PublicId}/estado", new { activo = true });
        var otraVez = await _http.GetFromJsonAsync<JsonElement>("api/clientes/buscar?ciNit=9999999");

        Assert.Equal(HttpStatusCode.NoContent, reactivar.StatusCode);
        Assert.Single(otraVez.EnumerateArray());
    }

    [Fact]
    public async Task Cambiar_estado_sin_indicar_activo_devuelve_400_y_no_da_de_baja()
    {
        var cliente = Repositorio.Agregar("9999999", "Universidad Mayor");

        var respuesta = await _http.PatchAsJsonAsync($"api/clientes/{cliente.PublicId}/estado", new { });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True(Repositorio.Clientes.Single().Estado);
    }

    [Fact]
    public async Task Cambiar_estado_de_un_cliente_inexistente_devuelve_404()
    {
        var respuesta = await _http.PatchAsJsonAsync($"api/clientes/{Guid.NewGuid()}/estado", new { activo = false });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Un_id_que_no_es_GUID_no_llega_al_controlador()
    {
        var respuesta = await _http.GetAsync("api/clientes/123");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage respuesta) =>
        JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;

    private static string[] Nombres(JsonElement clientes) =>
        clientes.EnumerateArray().Select(c => c.GetProperty("razonSocial").GetString()!).ToArray();
}

public sealed class ClientesApiBaseDeDatosCaidaTests
{
    [Fact]
    public async Task Si_MySQL_no_responde_devuelve_503_con_ProblemDetails()
    {
        using var fabrica = ClientesApiFactory.ConBaseDeDatosCaida();
        using var http = fabrica.CreateClient();

        var respuesta = await http.GetAsync("api/clientes");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(503, json.GetProperty("status").GetInt32());
        Assert.DoesNotContain("Password", json.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
