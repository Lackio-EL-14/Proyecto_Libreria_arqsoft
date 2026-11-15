namespace Libreria.Servicio.Ventas.Application.Models;

public sealed record DetalleComprobanteVenta(
    string NombreProducto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Importe);
