namespace Libreria.Application.Models;

// US-40: datos de trazabilidad listos para mostrar (nombres, no Ids).
public record AuditoriaRegistro(
    string? CreadoPor,
    DateTime? FechaCreacion,
    string? ModificadoPor,
    DateTime? FechaModificacion);
