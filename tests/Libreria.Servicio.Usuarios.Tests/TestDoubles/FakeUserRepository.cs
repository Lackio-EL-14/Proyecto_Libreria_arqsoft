using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Domain.Entities;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeUserRepository : IUserRepository
{
    public User? UserByUsername { get; set; }

    public string? ActivePasswordHash { get; set; }

    public bool UpdatePasswordResult { get; set; } = true;

    public bool SessionActive { get; set; }

    public Dictionary<int, string> Names { get; } = new();

    public HashSet<string> ExistingUsernames { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public int UpdatePasswordCalls { get; private set; }

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
}