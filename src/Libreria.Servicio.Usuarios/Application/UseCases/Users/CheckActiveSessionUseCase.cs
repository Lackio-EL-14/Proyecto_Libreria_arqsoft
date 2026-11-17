using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class CheckActiveSessionUseCase : ICheckActiveSessionUseCase
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