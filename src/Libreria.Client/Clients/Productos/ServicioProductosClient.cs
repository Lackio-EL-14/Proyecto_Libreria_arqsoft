using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;
using static Libreria.Client.Clients.Common.ConsultaHttp;

namespace Libreria.Client.Clients.Productos;

public sealed class ServicioProductosClient : ApiClientBase, IServicioProductosClient
{
    public ServicioProductosClient(HttpClient httpClient)
        : base(httpClient, "Productos")
    {
    }

    public async Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(string? busqueda, CancellationToken ct = default) =>
        await LeerAsync<List<CategoriaDto>>("api/categorias" + Armar(("busqueda", busqueda)), ct) ?? [];

    public Task<CategoriaDto?> ObtenerCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default) =>
        LeerOpcionalAsync<CategoriaDto>($"api/categorias/{publicId}?estadoEsperado={Booleano(activa)}", ct);

    public async Task<IReadOnlyList<string>> ObtenerUbicacionesCategoriaAsync(CancellationToken ct = default) =>
        await LeerAsync<List<string>>("api/categorias/ubicaciones", ct) ?? [];

    public Task<ResultadoApi> CrearCategoriaAsync(CategoriaInput input, CancellationToken ct = default) =>
        EnviarPostAsync("api/categorias", input, ct);

    public Task<ResultadoApi> ActualizarCategoriaAsync(Guid publicId, CategoriaInput input, CancellationToken ct = default) =>
        EnviarPutAsync($"api/categorias/{publicId}", input, ct);

    public Task<ResultadoApi> CambiarEstadoCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default) =>
        EnviarPatchAsync($"api/categorias/{publicId}/estado?nuevoEstado={Booleano(activa)}", ct);

    public async Task<IReadOnlyList<MarcaDto>> ObtenerMarcasAsync(string? nombre, CancellationToken ct = default) =>
        await LeerAsync<List<MarcaDto>>("api/marcas" + Armar(("nombre", nombre)), ct) ?? [];

    public Task<MarcaDto?> ObtenerMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default) =>
        LeerOpcionalAsync<MarcaDto>($"api/marcas/{publicId}?estadoEsperado={Booleano(activa)}", ct);

    public async Task<IReadOnlyList<string>> ObtenerPaisesMarcaAsync(CancellationToken ct = default) =>
        await LeerAsync<List<string>>("api/marcas/paises", ct) ?? [];

    public Task<ResultadoApi> CrearMarcaAsync(MarcaInput input, CancellationToken ct = default) =>
        EnviarPostAsync("api/marcas", input, ct);

    public Task<ResultadoApi> ActualizarMarcaAsync(Guid publicId, MarcaInput input, CancellationToken ct = default) =>
        EnviarPutAsync($"api/marcas/{publicId}", input, ct);

    public Task<ResultadoApi> CambiarEstadoMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default) =>
        EnviarPatchAsync($"api/marcas/{publicId}/estado?nuevoEstado={Booleano(activa)}", ct);

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
