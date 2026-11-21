using Libreria.Servicio.Usuarios.Domain;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface ICreateUserUseCase
{
    Task<Result<CreateUserResultDto>?> ExecuteAsync(CreateUserRequest request);
}
