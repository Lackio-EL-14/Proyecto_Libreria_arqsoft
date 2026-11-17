namespace Libreria.Servicio.Proveedores.Domain;

/// <summary>Contacto dependiente del proveedor, separado para mantener la tercera forma normal.</summary>
public sealed class ContactoProveedor
{
    public int ContactoProveedorId { get; set; }
    public int ProveedorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Cargo { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public bool EsPrincipal { get; set; } = true;
}
