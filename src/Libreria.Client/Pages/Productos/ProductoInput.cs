namespace Libreria.Client.Pages.Productos;

public class ProductoInput 
{
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? DescripcionEspecifica { get; set; }
    public int CategoriaId { get; set; }
    public int MarcaId { get; set; }
    public decimal CostoAdquisicion { get; set; }
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public bool EsPerecedero { get; set; }
    public System.DateTime? FechaVencimiento { get; set; }
}
