using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetActiveByUsernameAsync(string username);

    Task<string?> GetActivePasswordHashAsync(int userId);

    Task<bool> UpdatePasswordAsync(
        int userId,
        string previousHash,
        string newHash);

    Task<bool> IsSessionActiveAsync(int userId);

    Task<IReadOnlyDictionary<int, string>> GetFullNamesByIdsAsync(
    IReadOnlyCollection<int> userIds);

    Task<bool> UsernameExistsAsync(string username);

    Task<bool> IsActiveAdministratorAsync(int userId);

    Task<IReadOnlyList<UserListItemDto>> GetListAsync(
        string? status = null);

    Task<bool> EmailExistsAsync(string email);

    Task<int?> GetActiveRoleIdByNameAsync(string roleName);

    Task<IReadOnlyList<string>> GetActiveRoleNamesAsync();

    Task<Guid> CreatePendingAsync(User user);

    Task<User?> GetByActivationTokenHashAsync(string tokenHash);

    Task<User?> GetByPublicIdAsync(Guid publicId);

    Task<bool> UpdateActivationTokenAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt,
        int modifiedByUserId,
        DateTime modifiedAt);

    Task<bool> ActivateAsync(
        int userId,
        string passwordHash,
        DateTime activatedAt);

    Task<IReadOnlyList<UserDeactivationData>>
    GetUsersForDeactivationAsync();

    Task<bool> DeactivateAsync(
        int userId,
        int modifiedByUserId);
}
