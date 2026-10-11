using Libreria.Domain;
using Libreria.Application.Models;


namespace Libreria.Application.Ports.Primary;

public interface IVentaFacade
{
    Task<VentaRegistradaResult> RegistrarVentaAsync(
        RegistrarVentaInput input);

    Task AnularVentaAsync(Guid publicId);

    Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
        FiltroVentasInput filtro);

    Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(
        Guid publicId,
        bool esAdministrador);
}
