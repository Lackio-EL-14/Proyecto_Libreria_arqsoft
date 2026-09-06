namespace Libreria.Web.Pages.Productos.Models;

public record ProductoListItem(
    int ProductoId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    string Categoria,
    string Marca
);