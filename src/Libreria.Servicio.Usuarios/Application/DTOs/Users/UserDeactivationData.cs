namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record UserDeactivationData(
    int UserId,
    Guid PublicId,
    string Status,
    string Role,
    bool RoleActive);