using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Domain;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface IListUsersUseCase
{
    /// <summary><c>null</c> si el usuario actual no es Administrador activo; estado inválido → Result de validación.</summary>
    Task<Result<IReadOnlyList<UserListItemDto>>?> ExecuteAsync(string? status = null);
}
