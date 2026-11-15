namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record CreateUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Role);