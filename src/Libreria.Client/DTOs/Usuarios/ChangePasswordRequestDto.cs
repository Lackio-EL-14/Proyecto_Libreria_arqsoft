namespace Libreria.Client.DTOs.Usuarios;

public sealed record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword,
    string Confirmation);
