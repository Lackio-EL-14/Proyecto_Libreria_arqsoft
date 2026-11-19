using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Proveedores;

namespace Libreria.Client.Adapters.Proveedores;

/// <summary>
/// Patrón Adapter (US-140), interfaz destino: el puente de la UI hacia el MS Proveedores
/// (ruta <c>api/proveedores</c> del gateway). Las páginas dependen solo de esta interfaz.
/// Las lecturas lanzan <see cref="ServicioNoDisponibleException"/> si el MS no responde;
/// las escrituras devuelven <see cref="ResultadoApi"/> y nunca lanzan.
/// </summary>
public interface IProveedoresApi
{
    Task<IReadOnlyList<ProveedorDetalleDto>> ObtenerProveedoresAsync(
        FiltroProveedores filtro,
        CancellationToken cancellationToken = default);

    Task<ProveedorDetalleDto?> ObtenerProveedorAsync(
        Guid publicId,
        CancellationToken cancellationToken = default);

    Task<ResultadoApi> RegistrarProveedorAsync(
        ProveedorInput input,
        CancellationToken cancellationToken = default);

    Task<ResultadoApi> ActualizarProveedorAsync(
        Guid publicId,
        ProveedorInput input,
        CancellationToken cancellationToken = default);

    Task<ResultadoApi> CambiarEstadoProveedorAsync(
        Guid publicId,
        bool activo,
        CancellationToken cancellationToken = default);
}
