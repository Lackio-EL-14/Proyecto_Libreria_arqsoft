using Libreria.Client.Adapters.Proveedores;

namespace Libreria.Client.Clients.Proveedores;

/// <summary>Cliente tipado de US-113. Transitorio: lo reemplaza <c>ProveedoresApiAdapter</c> (US-140, hito B).</summary>
public interface IServicioProveedoresClient : IProveedoresApi
{
}
