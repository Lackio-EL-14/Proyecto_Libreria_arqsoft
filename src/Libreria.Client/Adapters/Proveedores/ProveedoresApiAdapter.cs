using Libreria.Client.Adapters.DependencyInjection;
using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Proveedores;
using static Libreria.Client.Clients.Common.ConsultaHttp;

namespace Libreria.Client.Adapters.Proveedores;

/// <summary>
/// Patrón Adapter (US-140): adapta el `HttpClient` «ApiGateway» a la interfaz destino
/// <see cref="IProveedoresApi"/>. Arma la URL, envía el token (handler de US-125) y traduce JSON,
/// `ProblemDetails` y códigos HTTP a DTOs del Front y a mensajes para la UI.
/// </summary>
public sealed class ProveedoresApiAdapter : ApiClientBase, IProveedoresApi
{
    private const string Ruta = "api/proveedores";

    public ProveedoresApiAdapter(IHttpClientFactory fabrica)
        : base(fabrica.CreateClient(ApiGateway.NombreCliente), "Proveedores")
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
