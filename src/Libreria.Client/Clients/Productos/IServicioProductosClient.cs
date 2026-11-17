using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;

namespace Libreria.Client.Clients.Productos;

/// <summary>
/// MS Productos detrás del gateway: productos, categorías, marcas e histórico de costos
/// (rutas <c>api/productos</c>, <c>api/categorias</c>, <c>api/marcas</c> y <c>api/historico</c>).
/// </summary>
public interface IServicioProductosClient : IApiClient
{
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(string? busqueda, CancellationToken ct = default);
    Task<CategoriaDto?> ObtenerCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ObtenerUbicacionesCategoriaAsync(CancellationToken ct = default);
    Task<ResultadoApi> CrearCategoriaAsync(CategoriaInput input, CancellationToken ct = default);
    Task<ResultadoApi> ActualizarCategoriaAsync(Guid publicId, CategoriaInput input, CancellationToken ct = default);
    Task<ResultadoApi> CambiarEstadoCategoriaAsync(Guid publicId, bool activa, CancellationToken ct = default);

    Task<IReadOnlyList<MarcaDto>> ObtenerMarcasAsync(string? nombre, CancellationToken ct = default);
    Task<MarcaDto?> ObtenerMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ObtenerPaisesMarcaAsync(CancellationToken ct = default);
    Task<ResultadoApi> CrearMarcaAsync(MarcaInput input, CancellationToken ct = default);
    Task<ResultadoApi> ActualizarMarcaAsync(Guid publicId, MarcaInput input, CancellationToken ct = default);
    Task<ResultadoApi> CambiarEstadoMarcaAsync(Guid publicId, bool activa, CancellationToken ct = default);

    Task<ProductoListadoDto> ObtenerProductosAsync(
        string? busqueda,
        Guid? categoriaPublicId,
        Guid? marcaPublicId,
        bool? estado = null,
        CancellationToken ct = default);
    Task<ProductoFormularioDto> ObtenerFormularioProductoAsync(CancellationToken ct = default);
    Task<ProductoDetalleDto?> ObtenerProductoAsync(Guid publicId, CancellationToken ct = default);
    Task<ProductoEdicionDto?> ObtenerProductoParaEdicionAsync(Guid publicId, CancellationToken ct = default);
    Task<ResultadoApi> RegistrarProductoAsync(ProductoInput input, CancellationToken ct = default);
    Task<ResultadoApi> ActualizarProductoAsync(Guid publicId, ProductoInput input, CancellationToken ct = default);
    Task<ResultadoApi> DarDeBajaProductoAsync(Guid publicId, CancellationToken ct = default);
    Task<ResultadoApi> ReactivarProductoAsync(Guid publicId, CancellationToken ct = default);

    Task<HistoricoProductoDto?> ObtenerHistoricoAsync(Guid productoPublicId, CancellationToken ct = default);
}
