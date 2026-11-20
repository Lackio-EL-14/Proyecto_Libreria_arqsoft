using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class ListUsersUseCase : IListUsersUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IProveedorIdentidad _proveedorIdentidad;

    public ListUsersUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser, IProveedorIdentidad proveedorIdentidad)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _proveedorIdentidad = proveedorIdentidad;
    }

    public async Task<IReadOnlyList<UserListItemDto>?> ExecuteAsync(
        string? status = null)
    {
        if (_currentUser.UserId is not Guid currentUserId ||
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

        // Sincronizar antes de filtrar: un Pendiente puede haber completado el cambio.
        var users = await _userRepository.GetListAsync();
        var result = new List<UserListItemDto>();
        foreach (var item in users)
        {
            var updated = item;
            if (item.Status != UserStatuses.Inactive)
            {
                var user = await _userRepository.GetByPublicIdAsync(item.PublicId);
                if (user?.KeycloakId is Guid keycloakId)
                {
                    var pending = await _proveedorIdentidad.TieneCambioDeContrasenaPendienteAsync(keycloakId);
                    if (!pending.EsExito)
                        throw new InvalidOperationException("No se pudo consultar el estado del usuario en Keycloak.");

                    if (!pending.Valor)
                        await _userRepository.ActivatePendingAsync(keycloakId);

                    updated = item with { Status = pending.Valor ? UserStatuses.Pending : UserStatuses.Active };
                }
            }
            if (normalizedStatus is null || updated.Status == normalizedStatus)
                result.Add(updated);
        }
        return result;
    }
}
