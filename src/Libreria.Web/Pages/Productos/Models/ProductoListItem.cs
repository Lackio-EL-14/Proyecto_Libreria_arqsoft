namespace Libreria.Web.Pages.Productos.Models;

public record ProductoListItem(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    string Categoria,
    string Marca,
    bool EsPerecedero
);