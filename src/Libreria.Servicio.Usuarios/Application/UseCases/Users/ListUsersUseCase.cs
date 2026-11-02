using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class ListUsersUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public ListUsersUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserListItemDto>?> ExecuteAsync(
        string? status = null)
    {
        if (_currentUser.UserId is not int currentUserId ||
            !await _userRepository.IsActiveAdministratorAsync(currentUserId))
        {
            return null;
        }

        var normalizedStatus =
            string.IsNullOrWhiteSpace(status)
                ? null
                : status.Trim();

        if (normalizedStatus is not null &&
            !UserStatuses.IsValid(normalizedStatus))
        {
            throw new ArgumentException(
                "Seleccione un estado de usuario válido.",
                nameof(status));
        }

        return await _userRepository.GetListAsync(normalizedStatus);
    }
}