using Libreria.Application.Ports.Secondary;

namespace Libreria.Application.Factories;

public abstract class UsuarioRepositoryFactory
{
    public abstract IUsuarioRepository CrearRepositorio();
}
