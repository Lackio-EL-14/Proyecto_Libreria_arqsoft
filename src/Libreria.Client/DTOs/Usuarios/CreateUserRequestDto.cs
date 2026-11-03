namespace Libreria.Client.DTOs.Usuarios;

public sealed record CreateUserRequestDto(
    string FirstName,
    string LastName,
    string Email,
    string Role);