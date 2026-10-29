using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Application.Services;

public class ComprobanteVentaService : IComprobanteVentaService
{
    private readonly IVentaRepository _ventas;
    private readonly IUsuarioActual _usuarioActual;

    public ComprobanteVentaService(
        IVentaRepository ventas,
        IUsuarioActual usuarioActual)
    {
        _ventas = ventas;
        _usuarioActual = usuarioActual;
    }

    public async Task<ComprobanteVenta?> ObtenerAsync(Guid publicId)
    {
        if (_usuarioActual.UsuarioId is not > 0)
        {
            throw new InvalidOperationException(
                "Debe iniciar sesión para consultar el comprobante.");
        }

        if (publicId == Guid.Empty)
        {
            return null;
        }

        var comprobante = await _ventas.ObtenerComprobanteAsync(publicId);

        if (comprobante is null)
        {
            return null;
        }

        return comprobante with
        {
            // Se mantiene la fecha y hora en que se genera el comprobante.
            GeneradoEl = DateTimeOffset.UtcNow.ToOffset(
                TimeSpan.FromHours(-4)),

            // El total ya viene persistido desde Venta.Total.
            TotalEnLetras = NumeroALetras.Convertir(comprobante.Total)
        };
    }
}