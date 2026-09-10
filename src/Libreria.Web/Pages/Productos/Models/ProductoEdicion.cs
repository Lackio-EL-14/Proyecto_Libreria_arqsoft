namespace Libreria.Web.Pages.Productos.Models;

public record ProductoEdicion(
    int ProductoId,
    ProductoInput Input,
    IReadOnlyList<CategoriaOption> Categorias,
    IReadOnlyList<MarcaOption> Marcas);
