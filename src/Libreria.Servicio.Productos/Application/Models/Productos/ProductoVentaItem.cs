namespace Libreria.Servicio.Productos.Application.Models.Productos;

public sealed record ProductoVentaItem(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta);