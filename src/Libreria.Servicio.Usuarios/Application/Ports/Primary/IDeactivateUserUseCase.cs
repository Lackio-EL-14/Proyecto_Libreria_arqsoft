using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface IDeactivateUserUseCase
{
    Task<DeactivateUserResult> ExecuteAsync(Guid publicId);
}
