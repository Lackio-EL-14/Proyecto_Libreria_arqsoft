namespace Libreria.Client.DTOs.Proveedores;

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
