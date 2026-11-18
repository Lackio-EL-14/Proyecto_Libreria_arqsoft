using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Proveedores;

namespace Libreria.Client.Clients.Proveedores;

/// <summary>Puerto del Front hacia el microservicio de Proveedores.</summary>
public interface IServicioProveedoresClient
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
