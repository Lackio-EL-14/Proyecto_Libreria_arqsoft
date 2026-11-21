using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Domain;
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

    /// <summary>Columnas por las que se puede ordenar el listado (Select con ordenación).</summary>
    public static readonly IReadOnlyDictionary<string, Func<UserListItemDto, string>> Columnas =
        new Dictionary<string, Func<UserListItemDto, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["nombre"] = user => user.FullName,
            ["usuario"] = user => user.Username,
            ["correo"] = user => user.Email,
            ["rol"] = user => user.Role,
            ["estado"] = user => user.Status
        };

    public async Task<Result<IReadOnlyList<UserListItemDto>>?> ExecuteAsync(
        string? status = null,
        string? orden = null,
        bool descendente = false)
    {
        if (!await IsAuthorizedAdministratorAsync())
        {
            return null;
        }

        var normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus is not null && !UserStatuses.IsValid(normalizedStatus))
        {
            return Result<IReadOnlyList<UserListItemDto>>.Fallo(
                Result.Validacion("estado", "Seleccione un estado de usuario válido."));
        }

        if (!string.IsNullOrWhiteSpace(orden) && !Columnas.ContainsKey(orden.Trim()))
        {
            return Result<IReadOnlyList<UserListItemDto>>.Fallo(
                Result.Validacion("orden", "Seleccione una columna válida para ordenar."));
        }

        var users = await RetrieveAndResolveUsersAsync(normalizedStatus);
        return Result<IReadOnlyList<UserListItemDto>>.Success(Ordenar(users, orden, descendente));
    }

    private static IReadOnlyList<UserListItemDto> Ordenar(
        IReadOnlyList<UserListItemDto> users,
        string? orden,
        bool descendente)
    {
        if (string.IsNullOrWhiteSpace(orden))
        {
            return users;
        }

        var clave = Columnas[orden.Trim()];
        var ordenados = descendente
            ? users.OrderByDescending(clave, StringComparer.CurrentCultureIgnoreCase)
            : users.OrderBy(clave, StringComparer.CurrentCultureIgnoreCase);
        return ordenados.ThenBy(user => user.Username, StringComparer.OrdinalIgnoreCase).ToList();
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
