using System.Net;
using System.Text;

namespace Libreria.Client.Tests.Clients;

/// <summary>Responde siempre lo mismo y guarda la última petición para verificar verbo y URL.</summary>
internal sealed class ManejadorHttpFalso(HttpStatusCode estado, string cuerpo = "") : HttpMessageHandler
{
    public HttpRequestMessage? UltimaPeticion { get; private set; }
    public string? UltimoCuerpo { get; private set; }

    public static HttpClient CrearHttp(HttpMessageHandler manejador) =>
        new(manejador) { BaseAddress = new Uri("http://localhost:5080/") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        UltimaPeticion = request;
        UltimoCuerpo = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(estado)
        {
            Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
        };
    }
}

internal sealed class ManejadorSinConexion : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("Conexión rechazada.");
}
