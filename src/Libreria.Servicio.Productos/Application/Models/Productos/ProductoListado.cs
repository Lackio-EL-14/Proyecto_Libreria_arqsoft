namespace Libreria.Servicio.Productos.Application.Models.Productos;

public sealed record ProductoListado(
    IReadOnlyList<ProductoListItem> Productos,
    IReadOnlyList<CategoriaProductoOption> Categorias,
    IReadOnlyList<MarcaProductoOption> Marcas);