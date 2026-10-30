using Libreria.Servicio.Usuarios.Domain.Entities;

namespace Libreria.Servicio.Usuarios.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetActiveByUsernameAsync(string username);

    Task<bool> UpdatePasswordAsync(
        int userId,
        string previousHash,
        string newHash);

    Task<bool> IsSessionActiveAsync(int userId);
}