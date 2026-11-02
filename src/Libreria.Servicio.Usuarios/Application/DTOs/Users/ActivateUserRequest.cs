namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record ActivateUserRequest(
    string TokenHash,
    string Token,
    string Password,
    string ConfirmPassword);