using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Application.Factories;

public abstract class UsuarioRepositoryFactory
{
    public abstract IUserRepository CreateRepository();
}