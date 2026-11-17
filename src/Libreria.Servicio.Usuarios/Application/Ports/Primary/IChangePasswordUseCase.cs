using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface IChangePasswordUseCase
{
    Task<bool> ExecuteAsync(ChangePasswordRequest request);
}
