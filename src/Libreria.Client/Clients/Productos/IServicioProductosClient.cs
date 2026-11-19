using Libreria.Client.Adapters.Productos;
using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Productos;

/// <summary>Cliente tipado de US-113. Transitorio: lo reemplaza <c>ProductosApiAdapter</c> (US-140, hito B).</summary>
public interface IServicioProductosClient : IProductosApi, IApiClient
{
}
