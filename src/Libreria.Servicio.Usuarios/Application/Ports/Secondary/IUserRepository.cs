using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Ports.Secondary;

public interface IUserRepository
{
    Task<bool> IsSessionActiveAsync(int userId);

    /// <summary>Usuario no dado de baja (Activo o Pendiente) y con rol activo, buscado por <c>KeycloakId</c> (el <c>sub</c> del token).</summary>
    Task<bool> IsSessionActiveAsync(Guid keycloakId);

    Task<IReadOnlyDictionary<int, string>> GetFullNamesByIdsAsync(
    IReadOnlyCollection<int> userIds);

    Task<IReadOnlyDictionary<Guid, string>> GetFullNamesByKeycloakIdsAsync(
        IReadOnlyCollection<Guid> publicIds);

    Task<bool> UsernameExistsAsync(string username);

    /// <summary>Administrador activo, buscado por <c>KeycloakId</c> (el <c>sub</c> del usuario actual).</summary>
    Task<bool> IsActiveAdministratorAsync(Guid keycloakId);

    Task<IReadOnlyList<UserListItemDto>> GetListAsync(
        string? status = null);

    Task<bool> EmailExistsAsync(string email);

    Task<int?> GetActiveRoleIdByNameAsync(string roleName);

    Task<IReadOnlyList<string>> GetActiveRoleNamesAsync();

    Task<Guid> CreatePendingAsync(User user);

    Task<User?> GetByPublicIdAsync(Guid publicId);

    Task<IReadOnlyList<UserDeactivationData>>
    GetUsersForDeactivationAsync();

    Task<bool> DeactivateAsync(
        int userId,
        Guid modifiedByUserId);
}
