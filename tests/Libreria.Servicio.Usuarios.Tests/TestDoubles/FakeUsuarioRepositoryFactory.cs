using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.Factories;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeUsuarioRepositoryFactory
    : UsuarioRepositoryFactory
{
    private readonly IUserRepository _repository;

    public FakeUsuarioRepositoryFactory(
        IUserRepository repository)
    {
        _repository = repository;
    }

    public override IUserRepository CreateRepository()
    {
        return _repository;
    }
}