using Libreria.Servicio.Proveedores.Application.Factories;
using Libreria.Servicio.Proveedores.Application.Ports.Secondary;
using Libreria.Servicio.Proveedores.Infrastructure.Repositories;

namespace Libreria.Servicio.Proveedores.Infrastructure.Factories;

/// <summary>Creador concreto del Factory Method para SQL Server.</summary>
public sealed class SqlProveedorRepositoryFactory : ProveedorRepositoryFactory
{
    private readonly IDbConnectionFactory _conexiones;
    private readonly IUsuarioActual _usuario;

    public SqlProveedorRepositoryFactory(IDbConnectionFactory conexiones, IUsuarioActual usuario)
    {
        _conexiones = conexiones;
        _usuario = usuario;
    }

    public override IProveedorRepository CrearRepositorio() =>
        new ProveedorRepository(_conexiones, _usuario);
}
