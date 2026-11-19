using Libreria.Client.Adapters.Clientes;
using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Clientes;

/// <summary>Cliente tipado de US-113. Transitorio: lo reemplaza <c>ClientesApiAdapter</c> (US-140, hito B).</summary>
public interface IServicioClientesClient : IClientesApi, IApiClient
{
}
