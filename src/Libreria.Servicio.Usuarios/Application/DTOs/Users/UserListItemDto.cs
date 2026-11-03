namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record UserListItemDto(
    Guid PublicId,
    string FullName,
    string Username,
    string Email,
    string Role,
    string Status);