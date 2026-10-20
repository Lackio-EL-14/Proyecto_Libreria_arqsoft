namespace Libreria.Application.Models;

public sealed record CacheDiagnostico(
    string Resultado,
    string Clave,
    string Replica,
    string Origen,
    long? TtlSegundos,
    double Milisegundos,
    DateTimeOffset FechaUtc);
