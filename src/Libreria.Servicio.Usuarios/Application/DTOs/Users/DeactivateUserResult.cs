namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public enum DeactivateUserResult
{
    Success,
    NotFound,
    ActorNotAuthorized,
    AlreadyInactive,
    LastActiveAdministrator,
    SelfDeactivation
}