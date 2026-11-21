namespace Libreria.Client.DTOs.Productos;

// DTOs propios del Front para el MS Productos.
// El Front no referencia el proyecto del servicio: solo conoce sus contratos JSON.

public record CategoriaDto(
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

public record MarcaDto(
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
