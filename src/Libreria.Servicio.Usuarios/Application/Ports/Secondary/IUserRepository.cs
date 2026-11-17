using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Ports.Secondary;

public interface IUserRepository
{
    Task<User?> GetActiveByUsernameAsync(string username);

    Task<string?> GetActivePasswordHashAsync(Guid userId);

    Task<bool> UpdatePasswordAsync(
        Guid userId,
        string previousHash,
        string newHash);

    Task<bool> IsSessionActiveAsync(int userId);

    Task<bool> IsSessionActiveAsync(Guid publicId);

    Task<IReadOnlyDictionary<int, string>> GetFullNamesByIdsAsync(
    IReadOnlyCollection<int> userIds);

    Task<IReadOnlyDictionary<Guid, string>> GetFullNamesByPublicIdsAsync(
        IReadOnlyCollection<Guid> publicIds);

    Task<bool> UsernameExistsAsync(string username);

    Task<bool> IsActiveAdministratorAsync(Guid userId);

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
