namespace Libreria.Servicio.Productos.Application.Models.Productos;

public sealed class ProductoInput
{
    public string Nombre { get; set; } = string.Empty;

    public string? DescripcionEspecifica { get; set; }

    public bool EsPerecedero { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int Stock { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal CostoAdquisicion { get; set; }

    public Guid CategoriaPublicId { get; set; }

    public Guid MarcaPublicId { get; set; }
}