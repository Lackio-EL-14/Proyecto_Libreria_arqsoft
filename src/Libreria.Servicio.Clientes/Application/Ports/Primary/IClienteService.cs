using Libreria.Servicio.Clientes.Application.Comun;
using Libreria.Servicio.Clientes.Application.Models;

namespace Libreria.Servicio.Clientes.Application.Ports.Primary;

/// <summary>Puerto primario: casos de uso de clientes que expone la API (US-115).</summary>
public interface IClienteService
{
    Task<IReadOnlyList<ClienteDetalle>> ListarAsync(FiltroClientes filtro);

    Task<Result<ClienteDetalle>> ObtenerAsync(Guid publicId);

    /// <summary>Clientes activos para la venta, por prefijo de CI/NIT.</summary>
    Task<Result<IReadOnlyList<ClienteVentaItem>>> BuscarParaVentaAsync(string? ciNit);

    Task<Result<ClienteDetalle>> RegistrarAsync(ClienteInput input);

    Task<Result<ClienteDetalle>> ActualizarAsync(Guid publicId, ClienteInput input);

    /// <summary>Delete lógico (<c>false</c>) o reactivación (<c>true</c>).</summary>
    Task<Result> CambiarEstadoAsync(Guid publicId, bool activo);
}
