using Libreria.Client.Adapters.Usuarios;
using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Usuarios;

/// <summary>Cliente tipado de US-113. Transitorio: lo reemplaza <c>UsuariosApiAdapter</c> (US-140, hito B).</summary>
public interface IServicioUsuariosClient : IUsuariosApi, IApiClient
{
}
