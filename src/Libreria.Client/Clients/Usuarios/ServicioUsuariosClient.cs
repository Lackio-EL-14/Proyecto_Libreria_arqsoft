using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Usuarios;

public sealed class ServicioUsuariosClient : ApiClientBase, IServicioUsuariosClient
{
    public ServicioUsuariosClient(HttpClient httpClient)
        : base(httpClient, "Usuarios")
    {
    }
}
