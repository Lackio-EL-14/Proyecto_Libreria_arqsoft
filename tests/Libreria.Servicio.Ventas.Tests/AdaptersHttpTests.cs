using System.Net;
using System.Net.Http.Json;
using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Infrastructure.Adapters;

namespace Libreria.Servicio.Ventas.Tests;

public class AdaptersHttpTests
{
    [Fact]
    public async Task ClientesHttpAdapter_SeleccionaCoincidenciaExacta()
    {
        var esperado = Guid.NewGuid();
        using var http = CrearHttp(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[]
            {
                new { publicId = Guid.NewGuid(), ciNit = "12340", razonSocial = "Otro" },
                new { publicId = esperado, ciNit = "1234", razonSocial = "Cliente exacto" }
            })
        });
        var adapter = new ClientesHttpAdapter(http);

        var resultado = await adapter.ObtenerPorCiNitAsync("1234");

        Assert.True(resultado.EsExito);
        Assert.Equal(esperado, resultado.Valor.PublicId);
        Assert.Equal("Cliente exacto", resultado.Valor.RazonSocial);
    }

    [Fact]
    public async Task ProductosHttpAdapter_StockInsuficiente_DevuelveConflicto()
    {
        using var http = CrearHttp(_ => new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = JsonContent.Create(new
            {
                title = "Stock insuficiente",
                detail = "No existe stock suficiente."
            })
        });
        var adapter = new ProductosHttpAdapter(http);

        var resultado = await adapter.DescontarStockAsync(
            Guid.NewGuid(),
            20,
            Guid.NewGuid());

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal("No existe stock suficiente.", resultado.Errores["Productos"]);
    }

    [Fact]
    public async Task ClientesHttpAdapter_SiNoHayConexion_DevuelveNoDisponible()
    {
        using var http = new HttpClient(new DelegadoHandler(_ =>
            throw new HttpRequestException("Sin conexión")))
        {
            BaseAddress = new Uri("http://clientes/")
        };
        var adapter = new ClientesHttpAdapter(http);

        var resultado = await adapter.ObtenerPorCiNitAsync("1234");

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoDisponible, resultado.Tipo);
    }

    private static HttpClient CrearHttp(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new DelegadoHandler(responder))
        {
            BaseAddress = new Uri("http://servicio/")
        };

    private sealed class DelegadoHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public DelegadoHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(_responder(request));
    }
}
