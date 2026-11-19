using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Domain;

namespace Libreria.Servicio.Clientes.Application.Ports.Secondary;

/// <summary>
/// Puerto secundario de persistencia de clientes. Lo implementa el adaptador MySQL
/// (<c>Infrastructure/Repositories/ClienteRepository</c>).
/// </summary>
public interface IClienteRepository : ICrudRepository<Cliente>
{
    /// <summary>Columna que admite <see cref="ICrudRepository{TEntity}.ExisteValorAsync"/>.</summary>
    const string ColumnaCiNit = "CiNit";

    /// <summary>Obtiene el cliente sin importar su estado (para editar o reactivar).</summary>
    Task<Cliente?> ObtenerPorPublicIdAsync(Guid publicId);

    Task<IReadOnlyList<Cliente>> ListarAsync(FiltroClientes filtro);

}
