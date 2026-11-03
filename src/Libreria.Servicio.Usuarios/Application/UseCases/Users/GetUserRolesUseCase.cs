using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class GetUserRolesUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetUserRolesUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<string>?> ExecuteAsync()
    {
        if (_currentUser.UserId is not int currentUserId ||
            !await _userRepository
                .IsActiveAdministratorAsync(currentUserId))
        {
            return null;
        }

        return await _userRepository
            .GetActiveRoleNamesAsync();
    }
}