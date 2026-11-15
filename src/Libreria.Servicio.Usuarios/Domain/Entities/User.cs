using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public Role Role { get; set; } = new();

    public string Status { get; set; } = UserStatuses.Pending;

    public string? ActivationTokenHash { get; set; }

    public DateTime? ActivationTokenExpiresAt { get; set; }

    public DateTime? ActivationTokenUsedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? ModifiedByUserId { get; set; }
}