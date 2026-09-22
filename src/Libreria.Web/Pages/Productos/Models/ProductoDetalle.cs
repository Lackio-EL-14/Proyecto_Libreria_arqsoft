namespace Libreria.Web.Pages.Productos.Models;

public record ProductoDetalle(
    int ProductoId,
    string Nombre,
    string? DescripcionEspecifica,
    bool EsPerecedero,
    DateTime? FechaVencimiento,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    int CategoriaId,
    string Categoria,
    int MarcaId,
    string Marca);
