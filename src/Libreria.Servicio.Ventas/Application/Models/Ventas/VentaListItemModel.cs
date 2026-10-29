namespace Libreria.Servicio.Ventas.Application.Models;

public class VentaListItemModel
{
    public Guid PublicId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteCiNit { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = "Activa";
    public string CreadoPor { get; set; } = string.Empty;
    public string? AnuladoPor { get; set; }
    public DateTime? FechaAnulacion { get; set; }
}
