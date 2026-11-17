using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;


namespace Libreria.Servicio.Ventas.Application.Ports.Primary;

public interface IVentaFacade
{
    Task<Result<VentaRegistradaResult>> RegistrarVentaAsync(
        RegistrarVentaInput input,
        CancellationToken cancellationToken = default);

    Task<Result> AnularVentaAsync(
        Guid publicId,
        string motivoAnulacion,
        CancellationToken cancellationToken = default);

    Task<Result> CorregirDatosAsync(
        Guid publicId,
        CorregirVentaInput input,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(
        FiltroVentasInput filtro);

    Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(Guid publicId);
}
