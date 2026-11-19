using System.Net;
using System.Net.Http.Json;
using Libreria.Gateway.Tests.Infraestructura;

namespace Libreria.Gateway.Tests.Seguridad;

/// <summary>US-126: el gateway valida el JWT antes de reenviar; sin token válido no llega nada al MS.</summary>
public sealed class SeguridadJwtGatewayTests : IClassFixture<ServiciosFalsos>, IAsyncLifetime
{
    private readonly GatewayFactory _gateway;
    private HttpClient _cliente = null!;

    public SeguridadJwtGatewayTests(ServiciosFalsos servicios)
    {
        _gateway = new GatewayFactory(servicios.Direcciones);
    }

    public Task InitializeAsync()
    {
        _cliente = _gateway.CreateClient();
        _cliente.DefaultRequestHeaders.Authorization = null;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _gateway.DisposeAsync();
    }

    public static TheoryData<string, string?> TokensInvalidos() => new()
    {
        { "sin token", null },
        { "firma alterada", TokensDePrueba.FirmaAlterada() },
        { "firmado con otra clave", TokensDePrueba.FirmadoConOtraClave() },
        { "vencido", TokensDePrueba.Vencido() },
        { "audiencia equivocada", TokensDePrueba.AudienciaEquivocada() },
        { "emisor equivocado", TokensDePrueba.EmisorEquivocado() },
        { "no es un JWT", "abc.def.ghi" }
    };

    [Theory]
    [MemberData(nameof(TokensInvalidos))]
    public async Task TokenInvalido_Responde401_SinReenviar(string caso, string? token)
    {
        using var respuesta = await EnviarAsync("/api/clientes", token);

        Assert.True(respuesta.StatusCode == HttpStatusCode.Unauthorized, caso);
        Assert.StartsWith("Bearer", respuesta.Headers.WwwAuthenticate.ToString());
        var problema = await respuesta.Content.ReadFromJsonAsync<Problema>();
        Assert.Equal(401, problema!.Status);
        Assert.Equal("No autenticado.", problema.Title);
    }

    [Fact]
    public async Task TokenVencido_LoDiceEnElDetalle()
    {
        using var respuesta = await EnviarAsync("/api/productos", TokensDePrueba.Vencido());

        var problema = await respuesta.Content.ReadFromJsonAsync<Problema>();
        Assert.Equal("El token de acceso venció. Inicie sesión de nuevo.", problema!.Detail);
    }

    [Theory]
    [InlineData("/api/clientes", "clientes")]
    [InlineData("/api/proveedores", "proveedores")]
    [InlineData("/api/productos", "productos")]
    [InlineData("/api/usuarios", "usuarios")]
    public async Task TokenValido_SeReenviaConElMismoBearer(string ruta, string servicio)
    {
        var token = TokensDePrueba.Vendedor();
        using var respuesta = await EnviarAsync(ruta, token);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var eco = await respuesta.Content.ReadFromJsonAsync<EcoDelServicio>();
        Assert.Equal(servicio, eco!.Servicio);
        Assert.Equal($"Bearer {token}", eco.Authorization);
    }

    [Fact]
    public async Task Salud_NoExigeToken()
    {
        using var respuesta = await EnviarAsync("/salud", token: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task RutaInexistente_SinToken_Responde401_NoRevelaLasRutas()
    {
        using var respuesta = await EnviarAsync("/api/reportes", token: null);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    private Task<HttpResponseMessage> EnviarAsync(string ruta, string? token)
    {
        var solicitud = new HttpRequestMessage(HttpMethod.Get, ruta);
        if (token is not null)
        {
            solicitud.Headers.Authorization = TokensDePrueba.Bearer(token);
        }

        return _cliente.SendAsync(solicitud);
    }

    private sealed record Problema(int Status, string Title, string? Detail);
}
