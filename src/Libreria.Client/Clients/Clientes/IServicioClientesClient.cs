using Libreria.Client.Clients.Common;

namespace Libreria.Client.Clients.Clientes;

/// <summary>
/// MS Clientes detrás del gateway (ruta <c>api/clientes</c>). Por ahora la venta lo usa
/// para el alta rápida; el CRUD tipado llega con US-115/US-116.
/// </summary>
public interface IServicioClientesClient : IApiClient
{
}
