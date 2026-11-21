using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Common;
using Libreria.Client.DTOs.Productos;

namespace Libreria.Client.Adapters.Productos;

/// <summary>
/// Patrón Adapter (US-140), interfaz destino: el puente de la UI hacia el MS Productos
/// (rutas <c>api/productos</c>, <c>api/categorias</c>, <c>api/marcas</c> y <c>api/historico</c>
/// del gateway). Las páginas dependen solo de esta interfaz.
/// Las lecturas lanzan <see cref="ServicioNoDisponibleException"/> si el MS no responde;
/// las escrituras devuelven <see cref="ResultadoApi"/> y nunca lanzan. Categorías y marcas
/// solo se consultan: un solo CRUD por microservicio (consigna del 17/11).
/// </summary>
public interface IProductosApi
{
    Task<IReadOnlyList<CategoriaDto>> ObtenerCategoriasAsync(string? busqueda, CancellationToken ct = default);

    Task<IReadOnlyList<MarcaDto>> ObtenerMarcasAsync(string? nombre, CancellationToken ct = default);

    Task<ProductoListadoDto> ObtenerProductosAsync(
        string? busqueda,
        Guid? categoriaPublicId,
        Guid? marcaPublicId,
        bool? estado = null,
        string? orden = null,
        bool descendente = false,
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
