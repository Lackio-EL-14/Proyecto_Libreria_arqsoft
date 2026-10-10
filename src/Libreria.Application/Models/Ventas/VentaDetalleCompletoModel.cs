namespace Libreria.Application.Models;

public class VentaDetalleCompletoModel
{
    public Guid PublicId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteCiNit { get; set; } = string.Empty;
    public string CreadoPor { get; set; } = string.Empty;
    public string? AnuladoPor { get; set; }
    public decimal Total { get; set; }
    public decimal? GananciaTotal { get; set; }
    public List<VentaLineaDetalleModel> Lineas { get; set; } = new();
}

public class VentaLineaDetalleModel
{
    public string ProductoNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public decimal? Ganancia { get; set; }
}
