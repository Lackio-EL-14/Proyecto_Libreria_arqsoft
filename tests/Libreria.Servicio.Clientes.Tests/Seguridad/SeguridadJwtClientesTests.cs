using System.Net;
using System.Net.Http.Json;
using Libreria.Servicio.Clientes.Tests.Api;

namespace Libreria.Servicio.Clientes.Tests.Seguridad;

/// <summary>US-126: el MS Clientes valida el JWT de Keycloak y aplica los roles en el servidor.</summary>
public sealed class SeguridadJwtClientesTests : IDisposable
{
    private readonly ClientesApiFactory _fabrica = ClientesApiFactory.ConRepositorioEnMemoria();
    private readonly HttpClient _http;

    public SeguridadJwtClientesTests()
    {
        _http = _fabrica.CreateClient();
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public void Dispose()
    {
        _http.Dispose();
        _fabrica.Dispose();
    }

    public static TheoryData<string, string?> TokensInvalidos() => new()
    {
        { "sin token", null },
        { "firma alterada", TokensDePrueba.FirmaAlterada() },
        { "firmado con otra clave", TokensDePrueba.FirmadoConOtraClave() },
        { "vencido", TokensDePrueba.Vencido() },
        { "audiencia equivocada", TokensDePrueba.AudienciaEquivocada() },
        { "emisor equivocado", TokensDePrueba.EmisorEquivocado() },
        { "sin sub", TokensDePrueba.Crear(Guid.Empty, "anonimo", ["Administrador"]) }
    };

    [Theory]
    [MemberData(nameof(TokensInvalidos))]
    public async Task TokenInvalido_Responde401(string caso, string? token)
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, "api/clientes", token);

        Assert.True(respuesta.StatusCode == HttpStatusCode.Unauthorized, caso);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Administrador_Lista_200()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, "api/clientes", TokensDePrueba.Admin());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Vendedor_GestionaClientes_200()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, "api/clientes", TokensDePrueba.Vendedor());

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Vendedor_NoPuedeCambiarElEstado_403()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Patch, $"api/clientes/{Guid.NewGuid()}/estado", TokensDePrueba.Vendedor(), new { activo = false });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<Problema>();
        Assert.Equal("Su rol no tiene permiso para esta operación.", problema!.Detail);
    }

    [Fact]
    public async Task Administrador_SiPuedeCambiarElEstado_NoEs403()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Patch, $"api/clientes/{Guid.NewGuid()}/estado", TokensDePrueba.Admin(), new { activo = false });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Salud_NoExigeToken()
    {
        using var respuesta = await EnviarAsync(HttpMethod.Get, "salud", token: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task Swagger_PublicaElEsquemaBearer()
    {
        var documento = await _http.GetStringAsync("swagger/v1/swagger.json");

        Assert.Contains("\"bearer\"", documento);
        Assert.Contains("\"securitySchemes\"", documento);
    }

    private Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string ruta, string? token, object? cuerpo = null)
    {
        var solicitud = new HttpRequestMessage(metodo, ruta);
        if (token is not null)
        {
            solicitud.Headers.Authorization = TokensDePrueba.Bearer(token);
        }

        if (cuerpo is not null)
        {
            solicitud.Content = JsonContent.Create(cuerpo);
        }

        return _http.SendAsync(solicitud);
    }

    private sealed record Problema(int Status, string Title, string? Detail);
}
