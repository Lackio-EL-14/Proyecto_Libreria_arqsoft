using Libreria.Client.Adapters.DependencyInjection;
using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using static Libreria.Client.Clients.Common.ConsultaHttp;

namespace Libreria.Client.Adapters.Productos;

/// <summary>
/// Patrón Adapter (US-140): adapta el `HttpClient` «ApiGateway» a la interfaz destino
/// <see cref="IProductosApi"/>. Arma la URL, envía el token (handler de US-125) y traduce JSON,
/// `ProblemDetails` y códigos HTTP a DTOs del Front y a mensajes para la UI.
/// </summary>
public sealed class ProductosApiAdapter : ApiClientBase, IProductosApi
{
    public ProductosApiAdapter(IHttpClientFactory fabrica)
        : base(fabrica.CreateClient(ApiGateway.NombreCliente), "Productos")
    {
    }

    public async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(string? busqueda, CancellationToken ct = default) =>
        await LeerAsync<List<CategoriaDto>>("api/categorias" + Armar(("busqueda", busqueda)), ct) ?? [];

    public async Task<IReadOnlyList<MarcaDto>> ObtenerMarcasAsync(string? nombre, CancellationToken ct = default) =>
        await LeerAsync<List<MarcaDto>>("api/marcas" + Armar(("nombre", nombre)), ct) ?? [];

    public async Task<ProductoListadoDto> ObtenerProductosAsync(
        string? busqueda,
        Guid? categoriaPublicId,
        Guid? marcaPublicId,
        bool? estado = null,
        CancellationToken ct = default)
    {
        var url = "api/productos" + Armar(
            ("busqueda", busqueda),
            ("categoriaPublicId", categoriaPublicId?.ToString()),
            ("marcaPublicId", marcaPublicId?.ToString()),
            ("estado", estado.HasValue ? Booleano(estado.Value) : null));

        return await LeerAsync<ProductoListadoDto>(url, ct)
            ?? new ProductoListadoDto([], [], []);
    }

    public async Task<ProductoFormularioDto> ObtenerFormularioProductoAsync(CancellationToken ct = default) =>
        await LeerAsync<ProductoFormularioDto>("api/productos/formulario", ct)
            ?? new ProductoFormularioDto([], []);

    public Task<ProductoDetalleDto?> ObtenerProductoAsync(Guid publicId, CancellationToken ct = default) =>
        LeerOpcionalAsync<ProductoDetalleDto>($"api/productos/{publicId}", ct);

    public Task<ProductoEdicionDto?> ObtenerProductoParaEdicionAsync(Guid publicId, CancellationToken ct = default) =>
        LeerOpcionalAsync<ProductoEdicionDto>($"api/productos/{publicId}/edicion", ct);

    public Task<ResultadoApi> RegistrarProductoAsync(ProductoInput input, CancellationToken ct = default) =>
        EnviarPostAsync("api/productos", input, ct);

    public Task<ResultadoApi> ActualizarProductoAsync(Guid publicId, ProductoInput input, CancellationToken ct = default) =>
        EnviarPutAsync($"api/productos/{publicId}", input, ct);

    public Task<ResultadoApi> DarDeBajaProductoAsync(Guid publicId, CancellationToken ct = default) =>
        EnviarPatchAsync($"api/productos/{publicId}/baja", ct);

    public Task<ResultadoApi> ReactivarProductoAsync(Guid publicId, CancellationToken ct = default) =>
        EnviarPatchAsync(
            $"api/productos/{publicId}/estado?nuevoEstado=true",
            ct);

    public Task<HistoricoProductoDto?> ObtenerHistoricoAsync(Guid productoPublicId, CancellationToken ct = default) =>
        LeerOpcionalAsync<HistoricoProductoDto>($"api/historico/{productoPublicId}", ct);
}
