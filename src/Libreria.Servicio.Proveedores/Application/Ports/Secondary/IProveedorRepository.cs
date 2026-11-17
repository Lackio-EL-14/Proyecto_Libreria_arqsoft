using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Domain;

namespace Libreria.Servicio.Proveedores.Application.Ports.Secondary;

public interface IProveedorRepository : ICrudRepository<Proveedor>
{
    const string ColumnaNit = "Nit";

    Task<Proveedor?> ObtenerPorPublicIdAsync(Guid publicId);

    Task<IReadOnlyList<Proveedor>> ListarAsync(FiltroProveedores filtro);
}
