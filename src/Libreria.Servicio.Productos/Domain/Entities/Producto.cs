namespace Libreria.Servicio.Productos.Domain.Entities;

public sealed class Producto
{
    public Guid PublicId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? DescripcionEspecifica { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public bool EsPerecedero { get; set; }
    public int Stock { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal CostoAdquisicionActual { get; set; }
    public bool Estado { get; set; }

    public ReferenciaCatalogo Categoria { get; set; } = new();
    public ReferenciaCatalogo Marca { get; set; } = new();

    public List<HistoricoCosto> HistoricoCostos { get; set; } = [];

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid? UsuarioCreacionId { get; set; }
    public Guid? UsuarioModificacionId { get; set; }
}