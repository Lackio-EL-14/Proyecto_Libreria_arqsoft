using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Domain;

namespace Libreria.Servicio.Clientes.Application.Factories;

/// <summary>
/// Creador abstracto de repositorios de clientes. Estrecha el tipo de retorno
/// (retorno covariante) para que el servicio reciba <see cref="IClienteRepository"/>
/// sin conocer la fábrica concreta de MySQL.
/// </summary>
public abstract class ClienteRepositoryFactory : CrudRepositoryFactory<Cliente>
{
    public abstract override IClienteRepository CrearRepositorio();
}
