namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record CreateUserResultDto(
    Guid PublicId,
    string Username);