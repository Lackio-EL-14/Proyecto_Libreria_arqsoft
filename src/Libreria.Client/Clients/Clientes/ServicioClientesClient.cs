using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Clientes;
using Libreria.Client.DTOs.Common;
using static Libreria.Client.Clients.Common.ConsultaHttp;

namespace Libreria.Client.Clients.Clientes;

/// <summary>Adapter: traduce las operaciones del Front a las rutas REST del MS Clientes.</summary>
public sealed class ServicioClientesClient : ApiClientBase, IServicioClientesClient
{
    private const string Ruta = "api/clientes";

    public ServicioClientesClient(HttpClient httpClient)
        : base(httpClient, "Clientes")
    {
    }

    public async Task<IReadOnlyList<ClienteDetalleDto>> ObtenerClientesAsync(
        FiltroClientes filtro, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        return await LeerAsync<List<ClienteDetalleDto>>(Ruta + ArmarConsulta(filtro), ct) ?? [];
    }

    public Task<ClienteDetalleDto?> ObtenerClienteAsync(Guid publicId, CancellationToken ct = default) =>
        LeerOpcionalAsync<ClienteDetalleDto>($"{Ruta}/{publicId}", ct);

    public Task<ResultadoApi> RegistrarClienteAsync(ClienteInput input, CancellationToken ct = default) =>
        EnviarPostAsync(Ruta, input, ct);

    public Task<ResultadoApi> ActualizarClienteAsync(Guid publicId, ClienteInput input, CancellationToken ct = default) =>
        EnviarPutAsync($"{Ruta}/{publicId}", input, ct);

    public Task<ResultadoApi> CambiarEstadoClienteAsync(Guid publicId, bool activo, CancellationToken ct = default) =>
        EnviarPatchAsync($"{Ruta}/{publicId}/estado", new CambioEstadoClienteDto(activo), ct);

    // "estado" no pasa por Armar: el MS entiende el parámetro vacío (estado=) como «todos»,
    // y Armar omite los valores vacíos.
    private static string ArmarConsulta(FiltroClientes filtro)
    {
        var consulta = Armar(
            ("busqueda", filtro.Busqueda),
            ("orden", filtro.Orden == OrdenClientes.RazonSocial ? null : filtro.Orden.ToString()),
            ("descendente", filtro.Descendente ? Booleano(true) : null));

        var estado = filtro.Estado switch
        {
            EstadoFiltroClientes.Inactivos => "estado=false",
            EstadoFiltroClientes.Todos => "estado=",
            _ => null
        };

        if (estado is null)
        {
            return consulta;
        }

        return consulta.Length == 0 ? "?" + estado : consulta + "&" + estado;
    }
}
