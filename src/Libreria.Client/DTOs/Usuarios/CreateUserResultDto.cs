namespace Libreria.Client.DTOs.Usuarios;

public sealed record CreateUserResultDto(
    Guid PublicId,
    string Username);