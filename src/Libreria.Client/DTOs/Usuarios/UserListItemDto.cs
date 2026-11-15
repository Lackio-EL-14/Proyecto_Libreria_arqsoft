namespace Libreria.Client.DTOs.Usuarios;

public sealed record UserListItemDto(
    Guid PublicId,
    string FullName,
    string Username,
    string Email,
    string Role,
    string Status);