using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
namespace Libreria.Servicio.Ventas.Application.Models;

public class HistoricoCostoItem
{
    public int HistoricoCostoId { get; set; }

    public decimal CostoNuevo { get; set; }

    public decimal? CostoAnterior { get; set; }
    public DateTime? FechaVencimiento { get; set; }

    public DateTime FechaVigencia { get; set; }

    public string? Motivo { get; set; }

    public string? RegistradoPor { get; set; }

    public decimal? VariacionAbsoluta =>
        CostoAnterior.HasValue
            ? CostoNuevo - CostoAnterior.Value
            : null;
}