namespace Libreria.Client.DTOs.Ventas;

public record VentaListItemDto(
    Guid PublicId,
    DateTime FechaCreacion,
    string ClienteNombre,
    string ClienteCiNit,
    decimal Total,
    string Estado,
    string CreadoPor,
    string? AnuladoPor,
    DateTime? FechaAnulacion);

public record VentaDetalleDto(
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
    IReadOnlyList<VentaLineaDto> Lineas);

public record VentaLineaDto(
    string ProductoNombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    decimal? Ganancia);

public class FiltroVentasModel
{
    public string? Estado { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
}
