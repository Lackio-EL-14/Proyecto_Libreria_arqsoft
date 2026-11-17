using Libreria.Servicio.Usuarios.Application.DTOs.Auth;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface ILoginUseCase
{
    Task<AuthenticatedUserDto?> ExecuteAsync(LoginRequest request);
}
