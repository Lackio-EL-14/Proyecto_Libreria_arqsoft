using Libreria.Client.DTOs.Productos;

namespace Libreria.Client.Pages.Productos;

/// <summary>Lo que necesita <c>_ProductoFormularioCampos</c>: el alta y la edición comparten el formulario.</summary>
public interface IProductoFormulario
{
    ProductoInput Input { get; }
    IReadOnlyList<CategoriaOptionDto> Categorias { get; }
    IReadOnlyList<MarcaOptionDto> Marcas { get; }
}
