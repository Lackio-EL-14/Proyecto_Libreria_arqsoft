using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
namespace Libreria.Servicio.Ventas.Application.Models;

public record ProductoListado(
    IReadOnlyList<ProductoListItem> Productos,
    IReadOnlyList<CategoriaOption> Categorias,
    IReadOnlyList<MarcaOption> Marcas);
