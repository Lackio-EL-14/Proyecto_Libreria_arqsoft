namespace Libreria.Servicio.Ventas.Application.Models;

// Entrada del caso de uso de producto. Es un modelo puro: el enlace de datos
// (decimales con punto o coma) es responsabilidad de cada adaptador de entrada,
// no del núcleo de la aplicación.
public class ProductoInput
{
    public string Nombre { get; set; } = string.Empty;

    public string? DescripcionEspecifica { get; set; }
    public bool EsPerecedero { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int Stock { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal CostoAdquisicion { get; set; }

    public int CategoriaId { get; set; }

    public int MarcaId { get; set; }
}
