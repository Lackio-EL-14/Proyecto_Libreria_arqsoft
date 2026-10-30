namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record UserActivationPreparation(
    string Username,
    string Token,
    string TokenHash,
    DateTime ExpiresAt);