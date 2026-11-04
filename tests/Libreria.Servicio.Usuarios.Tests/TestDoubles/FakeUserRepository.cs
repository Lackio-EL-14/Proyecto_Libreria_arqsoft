using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeUserRepository : IUserRepository
{
    public User? UserByUsername { get; set; }

    public string? ActivePasswordHash { get; set; }

    public bool UpdatePasswordResult { get; set; } = true;

    public bool SessionActive { get; set; }

    public bool IsActiveAdministrator { get; set; }

    public List<UserListItemDto> UserList { get; } = [];

    public string? LastListStatus { get; private set; }

    public Dictionary<int, string> Names { get; } = new();

    public bool EmailExists { get; set; }

    public Dictionary<string, int> ActiveRoles { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public User? ActivationUser { get; set; }

    public User? UserByPublicId { get; set; }

    public bool UpdateActivationTokenResult { get; set; } = true;

    public string? UpdatedActivationTokenHash { get; private set; }

    public DateTime? UpdatedActivationExpiresAt { get; private set; }

    public int? ActivationModifiedByUserId { get; private set; }

    public DateTime? ActivationModifiedAt { get; private set; }

    public User? CreatedUser { get; private set; }

    public bool ActivateResult { get; set; } = true;

    public int? ActivatedUserId { get; private set; }

    public string? ActivatedPasswordHash { get; private set; }

    public DateTime? ActivatedAt { get; private set; }

    public List<UserDeactivationData> UsersForDeactivation { get; } = [];

    public bool DeactivateResult { get; set; } = true;

    public int? DeactivatedUserId { get; private set; }

    public int? DeactivatedByUserId { get; private set; }

    public HashSet<string> ExistingUsernames { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public int UpdatePasswordCalls { get; private set; }

    public int? UpdatedPasswordUserId { get; private set; }

    public string? PreviousPasswordHash { get; private set; }

    public string? NewPasswordHash { get; private set; }

    public Task<User?> GetActiveByUsernameAsync(string username)
    {
        return Task.FromResult(UserByUsername);
    }

    public Task<string?> GetActivePasswordHashAsync(int userId)
    {
        return Task.FromResult(ActivePasswordHash);
    }

    public Task<bool> UpdatePasswordAsync(
        int userId,
        string previousHash,
        string newHash)
    {
        UpdatePasswordCalls++;
        UpdatedPasswordUserId = userId;
        PreviousPasswordHash = previousHash;
        NewPasswordHash = newHash;

        return Task.FromResult(UpdatePasswordResult);
    }

    public Task<bool> IsSessionActiveAsync(int userId)
    {
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

    public Task<bool> UsernameExistsAsync(string username)
    {
        return Task.FromResult(
            ExistingUsernames.Contains(username));
    }


    public Task<bool> IsActiveAdministratorAsync(int userId)
    {
        return Task.FromResult(IsActiveAdministrator);
    }

    public Task<IReadOnlyList<UserListItemDto>> GetListAsync(
        string? status = null)
    {
        LastListStatus = status;

        IReadOnlyList<UserListItemDto> result = UserList;

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

    public Task<User?> GetByActivationTokenHashAsync(
        string tokenHash)
    {
        return Task.FromResult(ActivationUser);
    }

    public Task<User?> GetByPublicIdAsync(Guid publicId)
    {
        return Task.FromResult(
            UserByPublicId?.PublicId == publicId
                ? UserByPublicId
                : null);
    }

    public Task<bool> UpdateActivationTokenAsync(
        int userId,
        string tokenHash,
        DateTime expiresAt,
        int modifiedByUserId,
        DateTime modifiedAt)
    {
        UpdatedActivationTokenHash = tokenHash;
        UpdatedActivationExpiresAt = expiresAt;
        ActivationModifiedByUserId = modifiedByUserId;
        ActivationModifiedAt = modifiedAt;

        return Task.FromResult(UpdateActivationTokenResult);
    }

    public Task<bool> ActivateAsync(
        int userId,
        string passwordHash,
        DateTime activatedAt)
    {
        ActivatedUserId = userId;
        ActivatedPasswordHash = passwordHash;
        ActivatedAt = activatedAt;

        return Task.FromResult(ActivateResult);
    }

    public Task<IReadOnlyList<UserDeactivationData>>
        GetUsersForDeactivationAsync()
    {
        return Task.FromResult<IReadOnlyList<UserDeactivationData>>(
            UsersForDeactivation);
    }

    public Task<bool> DeactivateAsync(
        int userId,
        int modifiedByUserId)
    {
        DeactivatedUserId = userId;
        DeactivatedByUserId = modifiedByUserId;

        return Task.FromResult(DeactivateResult);
    }

}
