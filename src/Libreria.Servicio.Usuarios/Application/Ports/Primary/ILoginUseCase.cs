using Libreria.Servicio.Usuarios.Application.Comun;
using Libreria.Servicio.Usuarios.Application.DTOs.Auth;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface ILoginUseCase
{
    Task<Result<AuthenticatedUserDto>> ExecuteAsync(LoginRequest request);
}
