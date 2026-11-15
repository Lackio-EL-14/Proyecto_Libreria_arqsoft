namespace Libreria.Servicio.Productos.Domain.Entities;

public sealed class HistoricoCosto
{
    public decimal CostoAdquisicion { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public decimal? TipoCambioUsd { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaVigencia { get; set; }
    public Guid UsuarioCreacionId { get; set; }
}