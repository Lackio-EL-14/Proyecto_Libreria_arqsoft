using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;

namespace Libreria.Application.Services;

public class ComprobanteVentaService : IComprobanteVentaService
{
    private readonly IVentaRepository _ventas;
    private readonly IUsuarioActual _usuarioActual;

    public ComprobanteVentaService(IVentaRepository ventas, IUsuarioActual usuarioActual)
    {
        _ventas = ventas;
        _usuarioActual = usuarioActual;
    }

    public async Task<ComprobanteVenta?> ObtenerAsync(Guid publicId, string generadoPor)
    {
        if (_usuarioActual.UsuarioId is not > 0)
        {
            throw new InvalidOperationException("Debe iniciar sesión para consultar el comprobante.");
        }

        if (publicId == Guid.Empty)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(generadoPor))
        {
            throw new ArgumentException("Debe identificar al usuario que genera el comprobante.", nameof(generadoPor));
        }

        var comprobante = await _ventas.ObtenerComprobanteAsync(publicId);
        if (comprobante is null)
        {
            return null;
        }

        return comprobante with
        {
            Total = comprobante.Detalles.Sum(detalle => detalle.Importe),
            // La fecha de generación no sustituye una fecha de venta que no fue persistida.
            FechaVenta = null,
            GeneradoEl = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-4)),
            GeneradoPor = generadoPor.Trim()
        };
    }
}
