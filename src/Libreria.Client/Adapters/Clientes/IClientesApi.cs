using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Clientes;
using Libreria.Client.DTOs.Common;

namespace Libreria.Client.Adapters.Clientes;

/// <summary>
/// Patrón Adapter (US-140), interfaz destino: el puente de la UI hacia el MS Clientes
/// (ruta <c>api/clientes</c> del gateway). Las páginas dependen solo de esta interfaz.
/// Las lecturas lanzan <see cref="ServicioNoDisponibleException"/> si el MS no responde;
/// las escrituras devuelven <see cref="ResultadoApi"/> y nunca lanzan.
/// </summary>
public interface IClientesApi
{
    Task<IReadOnlyList<ClienteDetalleDto>> ObtenerClientesAsync(FiltroClientes filtro, CancellationToken ct = default);

    Task<ClienteDetalleDto?> ObtenerClienteAsync(Guid publicId, CancellationToken ct = default);

    Task<ResultadoApi> RegistrarClienteAsync(ClienteInput input, CancellationToken ct = default);

    Task<ResultadoApi> ActualizarClienteAsync(Guid publicId, ClienteInput input, CancellationToken ct = default);

    Task<ResultadoApi> CambiarEstadoClienteAsync(Guid publicId, bool activo, CancellationToken ct = default);
}
