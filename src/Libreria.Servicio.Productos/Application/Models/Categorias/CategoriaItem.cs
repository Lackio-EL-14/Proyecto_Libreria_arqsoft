namespace Libreria.Servicio.Productos.Application.Models.Categorias;

public sealed record CategoriaItem(
    Guid PublicId,
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Ubicacion,
    bool Estado,
    Guid? UsuarioCreacionId,
    DateTime FechaCreacion,
    Guid? UsuarioModificacionId,
    DateTime? FechaModificacion,
    bool TieneProductosActivos);