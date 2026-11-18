using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Proveedores;
using static Libreria.Client.Clients.Common.ConsultaHttp;

namespace Libreria.Client.Clients.Proveedores;

/// <summary>Adaptador HTTP entre Razor Pages y el microservicio de Proveedores.</summary>
public sealed class ServicioProveedoresClient : ApiClientBase, IServicioProveedoresClient
{
    private const string Ruta = "api/proveedores";

    public ServicioProveedoresClient(HttpClient httpClient)
        : base(httpClient, "Proveedores")
    {
    }

    public async Task<IReadOnlyList<ProveedorDetalleDto>> ObtenerProveedoresAsync(
        FiltroProveedores filtro,
        CancellationToken cancellationToken = default) =>
        await LeerAsync<List<ProveedorDetalleDto>>(
            Ruta + ArmarConsulta(filtro),
            cancellationToken) ?? [];

    public Task<ProveedorDetalleDto?> ObtenerProveedorAsync(
        Guid publicId,
        CancellationToken cancellationToken = default) =>
        LeerOpcionalAsync<ProveedorDetalleDto>($"{Ruta}/{publicId}", cancellationToken);

    public Task<ResultadoApi> RegistrarProveedorAsync(
        ProveedorInput input,
        CancellationToken cancellationToken = default) =>
        EnviarPostAsync(Ruta, input, cancellationToken);

    public Task<ResultadoApi> ActualizarProveedorAsync(
        Guid publicId,
        ProveedorInput input,
        CancellationToken cancellationToken = default) =>
        EnviarPutAsync($"{Ruta}/{publicId}", input, cancellationToken);

    public Task<ResultadoApi> CambiarEstadoProveedorAsync(
        Guid publicId,
        bool activo,
        CancellationToken cancellationToken = default) =>
        EnviarPatchAsync(
            $"{Ruta}/{publicId}/estado",
            new CambioEstadoProveedorDto(activo),
            cancellationToken);

    private static string ArmarConsulta(FiltroProveedores filtro)
    {
        var consulta = Armar(
            ("busqueda", filtro.Busqueda),
            ("orden", filtro.Orden == OrdenProveedores.RazonSocial ? null : filtro.Orden.ToString()),
            ("descendente", filtro.Descendente ? Booleano(true) : null));

        var estado = filtro.Estado switch
        {
            EstadoFiltroProveedores.Inactivos => "estado=false",
            EstadoFiltroProveedores.Todos => "estado=",
            _ => null
        };

        if (estado is null)
        {
            return consulta;
        }

        return consulta.Length == 0 ? "?" + estado : consulta + "&" + estado;
    }
}
