namespace Libreria.Servicio.Productos.Application.Models.Productos;

public sealed record ProductoListItem(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    Guid CategoriaPublicId,
    string Categoria,
    Guid MarcaPublicId,
    string Marca,
    bool EsPerecedero);