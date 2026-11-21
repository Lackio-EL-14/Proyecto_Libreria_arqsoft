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

        // Un token válido de Keycloak prueba que ya cambió su contraseña temporal
        // (Keycloak no lo emite con UPDATE_PASSWORD pendiente): si seguía «Pendiente», se activa.
        await _userRepository.ActivatePendingAsync(currentUserId);
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
        await SynchronizePendingUsersAsync();
        return await _userRepository.GetListAsync(requestedStatus);
    }

    /// <summary>
    /// Los «Pendiente» que ya cambiaron su contraseña temporal en Keycloak pasan a «Activo» en la BD.
    /// Keycloak se consulta por <c>KeycloakId</c> (el <c>sub</c>), no por el <c>PublicId</c> de la BD.
    /// </summary>
    private async Task SynchronizePendingUsersAsync()
    {
        if (_proveedorIdentidad is null)
        {
            return;
        }

        foreach (var keycloakId in await _userRepository.GetPendingKeycloakIdsAsync())
        {
            var pendiente = await _proveedorIdentidad.TieneCambioDeContrasenaPendienteAsync(keycloakId);
            if (pendiente.IsSuccess && !pendiente.Valor)
            {
                await _userRepository.ActivatePendingAsync(keycloakId);
            }
        }
    }
}
