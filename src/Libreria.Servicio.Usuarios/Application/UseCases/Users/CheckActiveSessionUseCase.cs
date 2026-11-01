using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class CheckActiveSessionUseCase
{
    private readonly IUserRepository _userRepository;

    public CheckActiveSessionUseCase(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<bool> ExecuteAsync(int userId)
    {
        if (userId <= 0)
        {
            return Task.FromResult(false);
        }

        return _userRepository.IsSessionActiveAsync(userId);
    }
}