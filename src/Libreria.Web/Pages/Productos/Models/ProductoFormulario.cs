namespace Libreria.Web.Pages.Productos.Models;

public record ProductoFormulario(
    IReadOnlyList<CategoriaOption> Categorias,
    IReadOnlyList<MarcaOption> Marcas);
