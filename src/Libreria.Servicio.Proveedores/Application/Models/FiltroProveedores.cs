namespace Libreria.Servicio.Proveedores.Application.Models;

public enum OrdenProveedores { RazonSocial, Nit, Correo, FechaCreacion }

public sealed class FiltroProveedores
{
    public string? Busqueda { get; set; }
    public bool? Estado { get; set; } = true;
    public OrdenProveedores Orden { get; set; } = OrdenProveedores.RazonSocial;
    public bool Descendente { get; set; }
}
