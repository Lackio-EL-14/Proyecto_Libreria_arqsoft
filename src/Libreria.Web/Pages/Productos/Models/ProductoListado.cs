namespace Libreria.Web.Pages.Productos.Models;

public record ProductoListado(
    IReadOnlyList<ProductoListItem> Productos,
    IReadOnlyList<CategoriaOption> Categorias,
    IReadOnlyList<MarcaOption> Marcas);
