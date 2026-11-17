using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Clientes;
using Libreria.Client.DTOs.Common;

namespace Libreria.Client.Clients.Clientes;

/// <summary>
/// Puerto del Front hacia el MS Clientes detrás del gateway (ruta <c>api/clientes</c>).
/// Las lecturas lanzan <see cref="ServicioNoDisponibleException"/> si el MS no responde;
/// las escrituras devuelven <see cref="ResultadoApi"/> y nunca lanzan.
/// </summary>
public interface IServicioClientesClient : IApiClient
{
    Task<IReadOnlyList<ClienteDetalleDto>> ObtenerClientesAsync(FiltroClientes filtro, CancellationToken ct = default);

    Task<ClienteDetalleDto?> ObtenerClienteAsync(Guid publicId, CancellationToken ct = default);

    Task<ResultadoApi> RegistrarClienteAsync(ClienteInput input, CancellationToken ct = default);

    Task<ResultadoApi> ActualizarClienteAsync(Guid publicId, ClienteInput input, CancellationToken ct = default);

    Task<ResultadoApi> CambiarEstadoClienteAsync(Guid publicId, bool activo, CancellationToken ct = default);
}
