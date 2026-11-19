namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public sealed record UserDeactivationData(
    int UserId,
    Guid PublicId,
    Guid? KeycloakId,
    string Status,
    string Role,
    bool RoleActive);