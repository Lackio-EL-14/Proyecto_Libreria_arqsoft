namespace Libreria.Client.DTOs.Ventas;

public record HistoricoCostoDto(
    DateTime FechaVigencia,
    decimal CostoNuevo,
    decimal? CostoAnterior,
    decimal? VariacionAbsoluta,
    DateTime? FechaVencimiento,
    string? Motivo,
    string? RegistradoPor);

public record HistoricoProductoDto(
    Guid PublicId,
    string Nombre,
    bool Activo,
    IReadOnlyList<HistoricoCostoDto> Historial);
