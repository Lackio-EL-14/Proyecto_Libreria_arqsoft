namespace Libreria.Domain;

public class Usuario
{
    public int UsuarioId { get; set; }
    public Guid PublicId { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;

    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public int RolId { get; set; }
    public Rol Rol { get; set; } = new();

    public string Estado { get; set; } = EstadosUsuario.Pendiente;

    public string? TokenActivacionHash { get; set; }
    public DateTime? TokenActivacionExpiracion { get; set; }
    public DateTime? TokenActivacionFechaUso { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    public int? UsuarioCreacionId { get; set; }
    public int? UsuarioModificacionId { get; set; }
}