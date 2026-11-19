using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Application.Factories;

public abstract class UsuarioRepositoryFactory
{
    public abstract IUserRepository CreateRepository();
}