using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Clientes;

public sealed class ServicioClientesClient : ApiClientBase, IServicioClientesClient
{
    public ServicioClientesClient(HttpClient httpClient)
        : base(httpClient, "Clientes")
    {
    }
}
