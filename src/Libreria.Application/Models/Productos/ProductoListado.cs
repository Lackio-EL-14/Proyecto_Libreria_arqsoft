using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
namespace Libreria.Application.Models;

public record ProductoListado(
    IReadOnlyList<ProductoListItem> Productos,
    IReadOnlyList<CategoriaOption> Categorias,
    IReadOnlyList<MarcaOption> Marcas);
