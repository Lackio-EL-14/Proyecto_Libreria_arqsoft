namespace Libreria.Servicio.Ventas.Controllers.Dtos;

public record VentaListItemResponse(
    Guid PublicId,
    DateTime FechaCreacion,
    string ClienteNombre,
    string ClienteCiNit,
    decimal Total,
    string Estado,
    string CreadoPor,
    string? AnuladoPor,
    DateTime? FechaAnulacion);

public record VentaDetalleResponse(
    Guid PublicId,
    DateTime FechaCreacion,
    DateTime? FechaAnulacion,
    string? MotivoAnulacion,
    string Estado,
    string ClienteNombre,
    string ClienteCiNit,
    string CreadoPor,
    string? AnuladoPor,
    decimal Total,
    decimal? GananciaTotal,
    IReadOnlyList<VentaLineaResponse> Lineas);

public record VentaLineaResponse(
    string ProductoNombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    decimal? Ganancia);
