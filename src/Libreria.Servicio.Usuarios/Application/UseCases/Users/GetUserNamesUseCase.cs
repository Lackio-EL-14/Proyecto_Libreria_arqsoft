using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.Factories;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class GetUserNamesUseCase : IGetUserNamesUseCase
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
    public Task<IReadOnlyDictionary<Guid, string>> ExecuteAsync(
        IEnumerable<Guid> publicIds)
    {
        ArgumentNullException.ThrowIfNull(publicIds);

        return _userRepository.GetFullNamesByPublicIdsAsync(
            publicIds.Where(id => id != Guid.Empty).Distinct().ToArray());
    }
}