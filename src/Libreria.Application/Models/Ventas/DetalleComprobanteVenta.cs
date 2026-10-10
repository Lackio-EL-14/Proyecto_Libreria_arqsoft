namespace Libreria.Application.Models;

public sealed record DetalleComprobanteVenta(
    string NombreProducto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Importe);
