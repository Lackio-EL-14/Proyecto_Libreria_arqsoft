namespace Libreria.Servicio.Productos.Application.Models.Productos;

public sealed record ProductoFormulario(
    IReadOnlyList<CategoriaProductoOption> Categorias,
    IReadOnlyList<MarcaProductoOption> Marcas);