using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.Factories;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class GetUserNamesUseCase
{
    private readonly IUserRepository _userRepository;

    public GetUserNamesUseCase(
        UsuarioRepositoryFactory repositoryFactory)
    {
        _userRepository =
            repositoryFactory.CreateRepository();
    }

    public Task<IReadOnlyDictionary<int, string>> ExecuteAsync(
        IEnumerable<int> userIds)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var ids = userIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        return _userRepository
            .GetFullNamesByIdsAsync(ids);
    }
}