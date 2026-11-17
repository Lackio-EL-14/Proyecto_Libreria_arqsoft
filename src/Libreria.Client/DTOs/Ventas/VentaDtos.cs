namespace Libreria.Client.DTOs.Ventas;

public record VentaListItemDto(
    Guid PublicId,
    DateTime FechaCreacion,
    string ClienteNombre,
    string ClienteCiNit,
    decimal Total,
    decimal? GananciaTotal,
    string Estado,
    string CreadoPor,
    string? AnuladoPor,
    DateTime? FechaAnulacion);

public record VentaDetalleDto(
    Guid PublicId,
    DateTime FechaCreacion,
    DateTime? FechaAnulacion,
    string? MotivoAnulacion,
    string? Observacion,
    string Estado,
    string ClienteNombre,
    string ClienteCiNit,
    string CreadoPor,
    string? AnuladoPor,
    string? ModificadoPor,
    DateTime? FechaModificacion,
    decimal Total,
    decimal? GananciaTotal,
    IReadOnlyList<VentaLineaDto> Lineas);

public record VentaLineaDto(
    string ProductoNombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    decimal? Ganancia);

public record ComprobanteVentaDto(
    Guid PublicId,
    string Estado,
    string CiNitCliente,
    string RazonSocialCliente,
    IReadOnlyList<DetalleComprobanteDto> Detalles,
    decimal Total,
    string TotalEnLetras,
    DateTimeOffset? FechaVenta,
    DateTimeOffset GeneradoEl,
    string GeneradoPor);

public record DetalleComprobanteDto(
    string NombreProducto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Importe);

public class FiltroVentasModel
{
    public string? Estado { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public OrdenVentasDto Orden { get; set; } = OrdenVentasDto.Fecha;
    public bool Descendente { get; set; } = true;
}

public enum OrdenVentasDto
{
    Fecha,
    Total
}

public sealed record CorregirVentaDto(string CiNitCliente, string? Observacion);
