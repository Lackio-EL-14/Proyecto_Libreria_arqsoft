using System.Net;
using Libreria.Client.Clients.Common;

namespace Libreria.Client.Tests.Clients;

/// <summary>
/// Detrás del gateway (US-112), un microservicio caído llega como 502/503/504.
/// El cliente debe traducirlo a <see cref="ServicioNoDisponibleException"/> para que
/// <c>ServicioNoDisponibleFilter</c> lleve al usuario a la página ServicioNoDisponible.
/// </summary>
public sealed class ApiClientBaseTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task Un_error_del_gateway_se_traduce_a_servicio_no_disponible(HttpStatusCode estado)
    {
        var cliente = CrearCliente(estado);

        var error = await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => cliente.GetAsync<object>("api/clientes"));

        Assert.Equal("Clientes", error.Servicio);
        var causa = Assert.IsType<HttpRequestException>(error.InnerException);
        Assert.Equal(estado, causa.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Los_demas_errores_siguen_siendo_ApiClientException(HttpStatusCode estado)
    {
        var cliente = CrearCliente(estado);

        var error = await Assert.ThrowsAsync<ApiClientException>(
            () => cliente.GetAsync<object>("api/clientes"));

        Assert.Equal(estado, error.StatusCode);
    }

    [Fact]
    public async Task Un_error_de_red_sigue_siendo_servicio_no_disponible()
    {
        var cliente = new ClientePrueba(new HttpClient(new ManejadorQueFalla())
        {
            BaseAddress = new Uri("http://localhost:5080/")
        });

        await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => cliente.GetAsync<object>("api/clientes"));
    }

    private static ClientePrueba CrearCliente(HttpStatusCode estado) =>
        new(new HttpClient(new ManejadorConEstado(estado)) { BaseAddress = new Uri("http://localhost:5080/") });

    private sealed class ClientePrueba(HttpClient http) : ApiClientBase(http, "Clientes");

    private sealed class ManejadorConEstado(HttpStatusCode estado) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(estado) { Content = new StringContent(string.Empty) });
    }

    private sealed class ManejadorQueFalla : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Conexión rechazada.");
    }
}
