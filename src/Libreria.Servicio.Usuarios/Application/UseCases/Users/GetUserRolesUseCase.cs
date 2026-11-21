using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class GetUserRolesUseCase : IGetUserRolesUseCase
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
        if (_currentUser.UserId is not Guid currentUserId)
        {
            return null;
        }

        // Token válido ⇒ ya cambió su contraseña temporal: si seguía «Pendiente», se activa.
        await _userRepository.ActivatePendingAsync(currentUserId);
        if (!await _userRepository.IsActiveAdministratorAsync(currentUserId))
        {
            return null;
        }

        return await _userRepository
            .GetActiveRoleNamesAsync();
    }
}