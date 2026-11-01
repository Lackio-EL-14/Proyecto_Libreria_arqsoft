namespace Libreria.Servicio.Ventas.Controllers.Dtos;

// Contratos de respuesta REST del Servicio de Ventas (catálogos e histórico).
// Viven en la capa de entrada: el dominio no se serializa directamente y
// nunca se expone el id numérico interno de las entidades.

public record CategoriaResponse(
    Guid PublicId,
    string Codigo,
    string Nombre,
    string? Descripcion,
    string Ubicacion,
    bool Estado,
    int? UsuarioCreacionId,
    DateTime FechaCreacion,
    int? UsuarioModificacionId,
    DateTime? FechaModificacion,
    bool TieneProductosActivos);

public record MarcaResponse(
    Guid PublicId,
    string Nombre,
    string? Descripcion,
    string PaisOrigen,
    string? SitioWeb,
    bool Estado,
    int? UsuarioCreacionId,
    int? UsuarioModificacionId,
    bool TieneProductosActivos);

public record HistoricoCostoResponse(
    DateTime FechaVigencia,
    decimal CostoNuevo,
    decimal? CostoAnterior,
    decimal? VariacionAbsoluta,
    DateTime? FechaVencimiento,
    string? Motivo,
    string? RegistradoPor);

public record HistoricoProductoResponse(
    Guid PublicId,
    string Nombre,
    bool Activo,
    IReadOnlyList<HistoricoCostoResponse> Historial);
