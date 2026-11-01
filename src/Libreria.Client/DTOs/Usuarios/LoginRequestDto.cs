namespace Libreria.Client.DTOs.Usuarios;

public sealed record LoginRequestDto(
    string Username,
    string Password);
