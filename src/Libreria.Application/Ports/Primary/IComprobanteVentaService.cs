using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IComprobanteVentaService
{
    Task<ComprobanteVenta?> ObtenerAsync(Guid publicId, string generadoPor);
}
