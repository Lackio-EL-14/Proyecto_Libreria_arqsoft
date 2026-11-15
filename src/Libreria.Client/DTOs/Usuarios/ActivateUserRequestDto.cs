namespace Libreria.Client.DTOs.Usuarios;

public sealed record ActivateUserRequestDto(
    string TokenHash,
    string Token,
    string Password,
    string ConfirmPassword);