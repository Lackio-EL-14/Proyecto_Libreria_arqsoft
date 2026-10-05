namespace Libreria.Application.Domain;

public class Usuario
{
    public int UsuarioId { get; set; }
    public Guid PublicId { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RolId { get; set; }
    public Rol Rol { get; set; } = new();
    public bool Estado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
