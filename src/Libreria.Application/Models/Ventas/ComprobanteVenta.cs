namespace Libreria.Application.Models;

public sealed record ComprobanteVenta
{
    public Guid PublicId { get; init; }

    public string Estado { get; init; } = string.Empty;

    public string CiNitCliente { get; init; } = string.Empty;

    public string RazonSocialCliente { get; init; } = string.Empty;

    public IReadOnlyList<DetalleComprobanteVenta> Detalles { get; init; }
        = Array.Empty<DetalleComprobanteVenta>();

    public decimal Total { get; init; }

    // El esquema actual de Venta no almacena la fecha de registro.
    public DateTimeOffset? FechaVenta { get; init; }

    public DateTimeOffset GeneradoEl { get; init; }

    public string GeneradoPor { get; init; } = string.Empty;
}
