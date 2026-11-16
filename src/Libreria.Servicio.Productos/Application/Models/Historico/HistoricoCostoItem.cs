namespace Libreria.Servicio.Productos.Application.Models.Historico;

public sealed record HistoricoCostoItem(
    DateTime FechaVigencia,
    decimal CostoNuevo,
    decimal? CostoAnterior,
    decimal? VariacionAbsoluta,
    DateTime? FechaVencimiento,
    string? Motivo,
    string? RegistradoPor);