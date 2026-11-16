namespace Libreria.Servicio.Productos.Application.Models.Productos;

public sealed record ProductoDetalle(
    Guid PublicId,
    string Nombre,
    string? DescripcionEspecifica,
    bool EsPerecedero,
    DateTime? FechaVencimiento,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    Guid CategoriaPublicId,
    string Categoria,
    Guid MarcaPublicId,
    string Marca,
    Guid? UsuarioCreacionId,
    DateTime FechaCreacion,
    Guid? UsuarioModificacionId,
    DateTime? FechaModificacion);