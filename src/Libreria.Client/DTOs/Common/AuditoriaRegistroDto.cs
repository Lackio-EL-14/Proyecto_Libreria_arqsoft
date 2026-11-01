namespace Libreria.Client.DTOs.Common;

public sealed record AuditoriaRegistroDto(
    string? CreadoPor,
    DateTime? FechaCreacion,
    string? ModificadoPor,
    DateTime? FechaModificacion,
    string? AnuladoPor = null,
    DateTime? FechaAnulacion = null);
