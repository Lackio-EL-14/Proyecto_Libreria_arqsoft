namespace Libreria.Servicio.Usuarios.Application.DTOs.Auth;

public record AuthenticatedUserDto(
    int UserId,
    Guid PublicId,
    string Username,
    string FullName,
    string Role);