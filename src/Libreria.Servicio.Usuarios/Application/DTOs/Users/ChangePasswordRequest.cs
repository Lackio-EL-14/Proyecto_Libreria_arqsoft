namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string Confirmation);
