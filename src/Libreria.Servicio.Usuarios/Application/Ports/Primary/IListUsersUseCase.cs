using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface IListUsersUseCase
{
    Task<IReadOnlyList<UserListItemDto>?> ExecuteAsync(string? status = null);
}
