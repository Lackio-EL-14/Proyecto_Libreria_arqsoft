using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeUserRepository : IUserRepository
{
    public bool SessionActive { get; set; }

    public bool IsActiveAdministrator { get; set; }

    public List<UserListItemDto> UserList { get; } = [];

    public string? LastListStatus { get; private set; }

    public Dictionary<int, string> Names { get; } = new();

    public bool EmailExists { get; set; }

    public Dictionary<string, int> ActiveRoles { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public User? UserByPublicId { get; set; }

    public User? CreatedUser { get; private set; }

    public List<UserDeactivationData> UsersForDeactivation { get; } = [];

    public bool DeactivateResult { get; set; } = true;

    public int? DeactivatedUserId { get; private set; }

    public Guid? DeactivatedByUserId { get; private set; }

    public HashSet<string> ExistingUsernames { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsSessionActiveAsync(int userId)
    {
        return Task.FromResult(SessionActive);
    }

    public Guid? LastSessionPublicId { get; private set; }

    public Task<bool> IsSessionActiveAsync(Guid publicId)
    {
        LastSessionPublicId = publicId;
        return Task.FromResult(SessionActive);
    }

    public Task<IReadOnlyDictionary<int, string>> GetFullNamesByIdsAsync(
        IReadOnlyCollection<int> userIds)
    {
        IReadOnlyDictionary<int, string> result = Names
            .Where(pair => userIds.Contains(pair.Key))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value);

        return Task.FromResult(result);
    }

    public Dictionary<Guid, string> PublicNames { get; } = new();

    public Task<IReadOnlyDictionary<Guid, string>> GetFullNamesByKeycloakIdsAsync(
        IReadOnlyCollection<Guid> publicIds)
    {
        IReadOnlyDictionary<Guid, string> result = PublicNames
            .Where(pair => publicIds.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        return Task.FromResult(result);
    }

    public Task<bool> UsernameExistsAsync(string username)
    {
        return Task.FromResult(
            ExistingUsernames.Contains(username));
    }


    public Task<bool> IsActiveAdministratorAsync(Guid userId)
    {
        return Task.FromResult(IsActiveAdministrator);
    }

    public Task<IReadOnlyList<UserListItemDto>> GetListAsync(
        string? status = null)
    {
        LastListStatus = status;

        IReadOnlyList<UserListItemDto> result = UserList
            .Where(user => status is null || user.Status == status)
            .ToList();

        return Task.FromResult(result);
    }


    public Task<bool> EmailExistsAsync(string email)
    {
        return Task.FromResult(EmailExists);
    }

    public Task<int?> GetActiveRoleIdByNameAsync(
        string roleName)
    {
        return Task.FromResult(
            ActiveRoles.TryGetValue(roleName, out var roleId)
                ? (int?)roleId
                : null);
    }

    public Task<IReadOnlyList<string>>
        GetActiveRoleNamesAsync()
    {
        IReadOnlyList<string> roles =
            ActiveRoles.Keys
                .OrderBy(role => role)
                .ToArray();

        return Task.FromResult(roles);
    }

    public Task<Guid> CreatePendingAsync(User user)
    {
        CreatedUser = user;

        var publicId = user.PublicId == Guid.Empty
            ? Guid.NewGuid()
            : user.PublicId;

        return Task.FromResult(publicId);
    }

    public Task<User?> GetByPublicIdAsync(Guid publicId)
    {
        return Task.FromResult(
            UserByPublicId?.PublicId == publicId
                ? UserByPublicId
                : null);
    }

    public Task<IReadOnlyList<UserDeactivationData>>
        GetUsersForDeactivationAsync()
    {
        return Task.FromResult<IReadOnlyList<UserDeactivationData>>(
            UsersForDeactivation);
    }

    public Task<bool> DeactivateAsync(
        int userId,
        Guid modifiedByUserId)
    {
        DeactivatedUserId = userId;
        DeactivatedByUserId = modifiedByUserId;

        return Task.FromResult(DeactivateResult);
    }

    /// <summary>KeycloakId (sub) → PublicId de la BD. En la realidad son distintos para los usuarios nuevos.</summary>
    public Dictionary<Guid, Guid> PublicIdByKeycloakId { get; } = new();

    public List<Guid> ActivatedKeycloakIds { get; } = [];

    public List<Guid> PasswordChangesRegistered { get; } = [];

    public Task<IReadOnlyList<Guid>> GetPendingKeycloakIdsAsync()
    {
        IReadOnlyList<Guid> pending = PublicIdByKeycloakId
            .Where(pair => UserList.Any(user => user.PublicId == pair.Value && user.Status == "Pendiente"))
            .Select(pair => pair.Key)
            .ToList();

        return Task.FromResult(pending);
    }

    public Task<bool> ActivatePendingAsync(Guid keycloakId)
    {
        if (!PublicIdByKeycloakId.TryGetValue(keycloakId, out var publicId))
        {
            return Task.FromResult(false);
        }

        var index = UserList.FindIndex(user => user.PublicId == publicId && user.Status == "Pendiente");
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        UserList[index] = UserList[index] with { Status = "Activo" };
        ActivatedKeycloakIds.Add(keycloakId);
        return Task.FromResult(true);
    }

    public Task<bool> RegisterPasswordChangeAsync(Guid keycloakId)
    {
        PasswordChangesRegistered.Add(keycloakId);
        return Task.FromResult(true);
    }
}
