using Libreria.Servicio.Proveedores.Application.Ports.Secondary;
using Libreria.Servicio.Proveedores.Domain;

namespace Libreria.Servicio.Proveedores.Application.Factories;

public abstract class ProveedorRepositoryFactory : CrudRepositoryFactory<Proveedor>
{
    public abstract override IProveedorRepository CrearRepositorio();
}
