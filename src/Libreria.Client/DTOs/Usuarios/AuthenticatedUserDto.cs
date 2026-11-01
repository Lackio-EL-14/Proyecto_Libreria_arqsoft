namespace Libreria.Client.DTOs.Usuarios;

public sealed record AuthenticatedUserDto(
    int UserId,
    Guid PublicId,
    string Username,
    string FullName,
    string Role);
