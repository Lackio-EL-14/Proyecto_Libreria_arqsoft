using Libreria.Servicio.Ventas.Application.Models;

namespace Libreria.Servicio.Ventas.Application.Ports.Primary;

public interface IComprobanteVentaService
{
    Task<ComprobanteVenta?> ObtenerAsync(Guid publicId);
}
