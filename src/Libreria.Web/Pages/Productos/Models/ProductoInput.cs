using System.ComponentModel.DataAnnotations;

namespace Libreria.Web.Pages.Productos.Models;

public class ProductoInput
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    public string? DescripcionEspecifica { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "99999999.99",
        ErrorMessage = "El precio de venta no puede ser negativo.")]
    public decimal PrecioVenta { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "99999999.99",
        ErrorMessage = "El costo de adquisición no puede ser negativo.")]
    public decimal CostoAdquisicion { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Debe seleccionar una categoría.")]
    public int CategoriaId { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Debe seleccionar una marca.")]
    public int MarcaId { get; set; }
}