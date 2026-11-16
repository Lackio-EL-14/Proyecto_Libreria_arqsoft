namespace Libreria.Servicio.Productos.Application.Models.Marcas;

public sealed record MarcaItem(
    Guid PublicId,
    string Nombre,
    string? Descripcion,
    string PaisOrigen,
    string? SitioWeb,
    bool Estado,
    Guid? UsuarioCreacionId,
    DateTime FechaCreacion,
    Guid? UsuarioModificacionId,
    DateTime? FechaModificacion,
    bool TieneProductosActivos);