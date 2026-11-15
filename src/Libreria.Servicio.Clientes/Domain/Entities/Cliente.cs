namespace Libreria.Servicio.Clientes.Domain;

/// <summary>
/// Cliente de la librería. <see cref="ClienteId"/> es interno de esta BD;
/// hacia afuera solo viaja <see cref="PublicId"/> (contrato 6.5).
/// </summary>
public class Cliente
{
    public int ClienteId { get; set; }
    public Guid PublicId { get; set; }
    public string CiNit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Correo { get; set; }

    /// <summary>Delete lógico: <c>false</c> = inactivo (la fila no se borra).</summary>
    public bool Estado { get; set; } = true;

    // Auditoría: el `sub` del JWT (PublicId del usuario en MS Usuarios), sin FK.
    public DateTime FechaCreacion { get; set; }
    public Guid? UsuarioCreacionId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid? UsuarioModificacionId { get; set; }
}
