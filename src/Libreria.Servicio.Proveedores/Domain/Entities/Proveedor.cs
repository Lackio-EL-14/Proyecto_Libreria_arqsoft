namespace Libreria.Servicio.Proveedores.Domain;

/// <summary>Proveedor de productos. El identificador numérico nunca sale de su microservicio.</summary>
public sealed class Proveedor
{
    public int ProveedorId { get; set; }
    public Guid PublicId { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public bool Estado { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public Guid? UsuarioCreacionId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid? UsuarioModificacionId { get; set; }
    public ContactoProveedor? ContactoPrincipal { get; set; }
}
