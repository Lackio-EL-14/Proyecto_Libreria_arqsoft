namespace Libreria.Servicio.Ventas.Application.Models;

public sealed record ProductoVentaSnapshot(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicion);
