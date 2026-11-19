namespace Libreria.Servicio.Usuarios.Application.DTOs.Users;

public enum DeactivateUserResult
{
    Success,
    NotFound,
    ActorNotAuthorized,
    AlreadyInactive,
    LastActiveAdministrator,
    SelfDeactivation,

    /// <summary>Keycloak no respondió; la baja se revirtió (503).</summary>
    IdentityProviderUnavailable
}