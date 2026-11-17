using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface ICreateUserUseCase
{
    Task<CreateUserResultDto?> ExecuteAsync(CreateUserRequest request);
}
