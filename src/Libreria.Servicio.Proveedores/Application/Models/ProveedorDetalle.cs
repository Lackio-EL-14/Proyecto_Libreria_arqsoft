using Libreria.Servicio.Proveedores.Domain;

namespace Libreria.Servicio.Proveedores.Application.Models;

public sealed record ProveedorDetalle(Guid PublicId, string Nit, string RazonSocial, string? Telefono,
    string? Correo, bool Estado, string? NombreContacto, string? CargoContacto,
    string? TelefonoContacto, string? CorreoContacto, DateTime FechaCreacion,
    Guid? UsuarioCreacionId, DateTime? FechaModificacion, Guid? UsuarioModificacionId)
{
    public static ProveedorDetalle Desde(Proveedor p) => new(p.PublicId, p.Nit, p.RazonSocial,
        p.Telefono, p.Correo, p.Estado, p.ContactoPrincipal?.Nombre, p.ContactoPrincipal?.Cargo,
        p.ContactoPrincipal?.Telefono, p.ContactoPrincipal?.Correo, p.FechaCreacion,
        p.UsuarioCreacionId, p.FechaModificacion, p.UsuarioModificacionId);
}
