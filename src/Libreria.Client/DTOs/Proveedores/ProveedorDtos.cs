using System.ComponentModel.DataAnnotations;
using Libreria.Client.Validacion;

namespace Libreria.Client.DTOs.Proveedores;

// Alta y edición de proveedores. Los mensajes son los mismos de ProveedorValidator del
// MS Proveedores, que vuelve a validar todo (pruebas del 22/11, B2).
public sealed class ProveedorInput
{
    [Display(Name = "NIT")]
    [Required(ErrorMessage = "El NIT es obligatorio.")]
    [StringLength(30, ErrorMessage = "El NIT no puede superar los 30 caracteres.")]
    public string Nit { get; set; } = string.Empty;

    [Display(Name = "Razón social")]
    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(200, ErrorMessage = "La razón social no puede superar los 200 caracteres.")]
    public string RazonSocial { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [Display(Name = "Correo electrónico")]
    [StringLength(254, ErrorMessage = "El correo del proveedor no puede superar los 254 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo del proveedor no es válido.")]
    public string? Correo { get; set; }

    [Display(Name = "Nombre completo")]
    [Required(ErrorMessage = "El nombre del contacto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre del contacto no puede superar los 150 caracteres.")]
    public string NombreContacto { get; set; } = string.Empty;

    [Display(Name = "Cargo")]
    [StringLength(100, ErrorMessage = "El cargo del contacto no puede superar los 100 caracteres.")]
    public string? CargoContacto { get; set; }

    [Display(Name = "Teléfono")]
    [StringLength(30, ErrorMessage = "El teléfono del contacto no puede superar los 30 caracteres.")]
    [RequeridoAlgunoDe(nameof(CorreoContacto),
        ErrorMessage = "Ingrese al menos un teléfono o correo para el contacto principal.")]
    public string? TelefonoContacto { get; set; }

    [Display(Name = "Correo electrónico")]
    [StringLength(254, ErrorMessage = "El correo del contacto no puede superar los 254 caracteres.")]
    [EmailAddress(ErrorMessage = "El correo del contacto no es válido.")]
    public string? CorreoContacto { get; set; }
}

public sealed record ProveedorDetalleDto(
    Guid PublicId,
    string Nit,
    string RazonSocial,
    string? Telefono,
    string? Correo,
    bool Estado,
    string? NombreContacto,
    string? CargoContacto,
    string? TelefonoContacto,
    string? CorreoContacto,
    DateTime FechaCreacion,
    Guid? UsuarioCreacionId,
    DateTime? FechaModificacion,
    Guid? UsuarioModificacionId);

public enum EstadoFiltroProveedores
{
    Activos,
    Inactivos,
    Todos
}

public enum OrdenProveedores
{
    RazonSocial,
    Nit,
    Correo,
    FechaCreacion
}

public sealed record FiltroProveedores(
    string? Busqueda = null,
    EstadoFiltroProveedores Estado = EstadoFiltroProveedores.Activos,
    OrdenProveedores Orden = OrdenProveedores.RazonSocial,
    bool Descendente = false);

public sealed record CambioEstadoProveedorDto(bool Activo);
