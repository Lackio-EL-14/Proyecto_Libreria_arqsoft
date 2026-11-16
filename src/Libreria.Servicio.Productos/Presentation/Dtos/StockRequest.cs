namespace Libreria.Servicio.Productos.Presentation.Dtos;

public sealed record StockRequest(
    int Cantidad,
    Guid VentaPublicId);