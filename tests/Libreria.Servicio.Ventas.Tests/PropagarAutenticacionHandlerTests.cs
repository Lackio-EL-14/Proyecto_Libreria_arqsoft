using System.Net;
using Libreria.Servicio.Ventas.Infrastructure.Http;
using Microsoft.AspNetCore.Http;

namespace Libreria.Servicio.Ventas.Tests;

public class PropagarAutenticacionHandlerTests
{
    [Fact]
    public async Task SendAsync_PropagaJwtYEncabezadosDeDesarrollo()
    {
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers.Authorization = "Bearer token-prueba";
        contexto.Request.Headers["X-Usuario-Id"] = Guid.NewGuid().ToString();
        contexto.Request.Headers["X-Usuario-Nombre"] = "cesar";

        HttpRequestMessage? capturada = null;
        var handler = new PropagarAutenticacionHandler(new HttpContextAccessor
        {
            HttpContext = contexto
        })
        {
            InnerHandler = new DelegadoHandler(request =>
            {
                capturada = request;
                return new HttpResponseMessage(HttpStatusCode.OK);
            })
        };
        using var client = new HttpClient(handler);

        await client.GetAsync("http://productos/api/productos");

        Assert.NotNull(capturada);
        Assert.Equal("Bearer", capturada.Headers.Authorization?.Scheme);
        Assert.Equal("token-prueba", capturada.Headers.Authorization?.Parameter);
        Assert.Equal("cesar", capturada.Headers.GetValues("X-Usuario-Nombre").Single());
    }

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
