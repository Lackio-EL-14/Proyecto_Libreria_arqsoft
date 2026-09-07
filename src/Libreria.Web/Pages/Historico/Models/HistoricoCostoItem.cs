namespace Libreria.Web.Pages.Historico.Models;

public class HistoricoCostoItem
{
    public int HistoricoCostoId { get; set; }

    public decimal CostoNuevo { get; set; }

    public decimal? CostoAnterior { get; set; }

    public DateTime FechaVigencia { get; set; }

    public string? Motivo { get; set; }

    public decimal? VariacionAbsoluta =>
        CostoAnterior.HasValue
            ? CostoNuevo - CostoAnterior.Value
            : null;
}