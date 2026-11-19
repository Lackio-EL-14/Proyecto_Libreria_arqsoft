namespace Libreria.Servicio.Proveedores.Application.Models;

public sealed class ProveedorInput
{
    public string Nit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string NombreContacto { get; set; } = string.Empty;
    public string? CargoContacto { get; set; }
    public string? TelefonoContacto { get; set; }
    public string? CorreoContacto { get; set; }
}
