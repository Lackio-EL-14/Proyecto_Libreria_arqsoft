using Libreria.Client.DTOs.Ventas;

namespace Libreria.Client.Clients;

// Puerto del Front hacia el Servicio de Ventas (SBA). Las páginas dependen de
// esta interfaz y no de HttpClient: así se pueden probar y reemplazar.
public interface IVentasApiClient
{
    // Categorías
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(string? busqueda, CancellationToken ct = default);
    Task<CategoriaDto?> ObtenerCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ObtenerUbicacionesCategoriaAsync(CancellationToken ct = default);
    Task<ResultadoApi> CrearCategoriaAsync(CategoriaInput input, CancellationToken ct = default);
    Task<ResultadoApi> ActualizarCategoriaAsync(Guid publicId, CategoriaInput input, CancellationToken ct = default);
    Task<ResultadoApi> CambiarEstadoCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default);

    // Marcas
    Task<IReadOnlyList<MarcaDto>> ObtenerMarcasAsync(string? nombre, CancellationToken ct = default);
    Task<MarcaDto?> ObtenerMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ObtenerPaisesMarcaAsync(CancellationToken ct = default);
    Task<ResultadoApi> CrearMarcaAsync(MarcaInput input, CancellationToken ct = default);
    Task<ResultadoApi> ActualizarMarcaAsync(Guid publicId, MarcaInput input, CancellationToken ct = default);
    Task<ResultadoApi> CambiarEstadoMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default);

    // Productos
    Task<ProductoListadoDto> ObtenerProductosAsync(string? busqueda, int? categoriaId, int? marcaId, CancellationToken ct = default);
    Task<ProductoFormularioDto> ObtenerFormularioProductoAsync(CancellationToken ct = default);
    Task<ProductoDetalleDto?> ObtenerProductoAsync(Guid publicId, CancellationToken ct = default);
    Task<ProductoEdicionDto?> ObtenerProductoParaEdicionAsync(Guid publicId, CancellationToken ct = default);
    Task<ResultadoApi> RegistrarProductoAsync(ProductoInput input, CancellationToken ct = default);
    Task<ResultadoApi> ActualizarProductoAsync(Guid publicId, ProductoInput input, CancellationToken ct = default);
    Task<ResultadoApi> DarDeBajaProductoAsync(Guid publicId, CancellationToken ct = default);

    // Histórico de costos
    Task<HistoricoProductoDto?> ObtenerHistoricoAsync(Guid productoPublicId, CancellationToken ct = default);
}
