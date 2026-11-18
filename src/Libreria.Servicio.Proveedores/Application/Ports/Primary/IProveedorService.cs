using Libreria.Servicio.Proveedores.Domain;
using Libreria.Servicio.Proveedores.Application.Models;
namespace Libreria.Servicio.Proveedores.Application.Ports.Primary;

public interface IProveedorService
{
    Task<IReadOnlyList<ProveedorDetalle>> ListarAsync(FiltroProveedores filtro);
    Task<Result<ProveedorDetalle>> ObtenerAsync(Guid publicId);
    Task<Result<ProveedorDetalle>> RegistrarAsync(ProveedorInput input);
    Task<Result<ProveedorDetalle>> ActualizarAsync(Guid publicId, ProveedorInput input);
    Task<Result> CambiarEstadoAsync(Guid publicId, bool activo);
}
