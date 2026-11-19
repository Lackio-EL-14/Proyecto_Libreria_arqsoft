using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    /// <summary>Id del usuario en Keycloak (el <c>sub</c> del token). Null hasta darlo de alta allí (US-130).</summary>
    public Guid? KeycloakId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? MaternalLastName { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role Role { get; set; } = new();

    public string Status { get; set; } = UserStatuses.Pending;

    public DateTime? TemporaryPasswordExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? ModifiedByUserId { get; set; }
}