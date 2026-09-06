namespace Libreria.Web.Pages.Productos.Models;

public record ProductoDetalle(
    int ProductoId,
    string Nombre,
    string? DescripcionEspecifica,
    DateTime? FechaVencimiento,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    int CategoriaId,
    string Categoria,
    int MarcaId,
    string Marca)
{
    public ProductoInput CrearInputEdicion()
    {
        return new ProductoInput
        {
            Nombre = Nombre,
            DescripcionEspecifica = DescripcionEspecifica,
            FechaVencimiento = FechaVencimiento,
            Stock = Stock,
            PrecioVenta = PrecioVenta,
            CostoAdquisicion = CostoAdquisicionActual,
            CategoriaId = CategoriaId,
            MarcaId = MarcaId
        };
    }
}
