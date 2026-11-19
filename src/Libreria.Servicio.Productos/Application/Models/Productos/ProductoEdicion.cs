namespace Libreria.Servicio.Productos.Application.Models.Productos;

public sealed record ProductoEdicion(
    ProductoInput Input,
    IReadOnlyList<CategoriaProductoOption> Categorias,
    IReadOnlyList<MarcaProductoOption> Marcas);