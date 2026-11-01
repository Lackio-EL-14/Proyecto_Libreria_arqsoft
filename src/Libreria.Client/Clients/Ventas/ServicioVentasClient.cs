using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Ventas;

public sealed class ServicioVentasClient : ApiClientBase, IServicioVentasClient
{
    public ServicioVentasClient(HttpClient httpClient)
        : base(httpClient, "Ventas")
    {
    }
}
