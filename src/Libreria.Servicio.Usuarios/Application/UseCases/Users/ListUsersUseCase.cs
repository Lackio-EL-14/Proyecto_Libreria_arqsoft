using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class ListUsersUseCase : IListUsersUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IProveedorIdentidad? _proveedorIdentidad;

    public ListUsersUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IProveedorIdentidad? proveedorIdentidad = null)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _proveedorIdentidad = proveedorIdentidad;
    }

    public async Task<IReadOnlyList<UserListItemDto>?> ExecuteAsync(string? status = null)
    {
        if (!await IsAuthorizedAdministratorAsync())
        {
            return null;
        }

        var normalizedStatus = NormalizeStatus(status);
        EnsureValidStatus(normalizedStatus);

        return await RetrieveAndResolveUsersAsync(normalizedStatus);
    }

    private async Task<bool> IsAuthorizedAdministratorAsync()
    {
        if (_currentUser.UserId is not Guid currentUserId)
        {
            return false;
        }

        return await _userRepository.IsActiveAdministratorAsync(currentUserId);
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim();
    }

    private static void EnsureValidStatus(string? status)
    {
        if (status is not null && !UserStatuses.IsValid(status))
        {
            throw new ArgumentException("Seleccione un estado de usuario válido.", nameof(status));
        }
    }

    private async Task<IReadOnlyList<UserListItemDto>> RetrieveAndResolveUsersAsync(string? requestedStatus)
    {
        if (_proveedorIdentidad is null)
        {
            return await _userRepository.GetListAsync(requestedStatus);
        }

        var rawUsers = await _userRepository.GetListAsync();
        return await ResolveStatusesAndFilterAsync(rawUsers, requestedStatus);
    }

    private async Task<IReadOnlyList<UserListItemDto>> ResolveStatusesAndFilterAsync(
        IReadOnlyList<UserListItemDto> rawUsers,
        string? requestedStatus)
    {
        var resolvedUsers = new List<UserListItemDto>(rawUsers.Count);

        foreach (var user in rawUsers)
        {
            var resolved = await ResolveSingleUserAsync(user);
            resolvedUsers.Add(resolved);
        }

        return FilterUsersByStatus(resolvedUsers, requestedStatus);
    }

    private async Task<UserListItemDto> ResolveSingleUserAsync(UserListItemDto user)
    {
        if (user.Status != UserStatuses.Pending)
        {
            return user;
        }

        return await CheckPasswordPendingAsync(user);
    }

    private async Task<UserListItemDto> CheckPasswordPendingAsync(UserListItemDto user)
    {
        var result = await _proveedorIdentidad!.TieneCambioDeContrasenaPendienteAsync(user.PublicId);

        if (result.IsSuccess && !result.Valor)
        {
            return user with { Status = UserStatuses.Active };
        }

        return user;
    }

    private static IReadOnlyList<UserListItemDto> FilterUsersByStatus(
        IReadOnlyList<UserListItemDto> users,
        string? requestedStatus)
    {
        if (requestedStatus is null)
        {
            return users;
        }

        return users.Where(user => user.Status == requestedStatus).ToList();
    }
}
