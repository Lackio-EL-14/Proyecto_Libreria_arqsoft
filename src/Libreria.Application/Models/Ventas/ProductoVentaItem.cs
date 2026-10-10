namespace Libreria.Application.Models;

public record ProductoVentaItem(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta);
