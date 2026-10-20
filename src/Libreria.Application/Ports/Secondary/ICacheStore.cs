namespace Libreria.Application.Ports.Secondary;

public interface ICacheStore
{
    Task<T?> ObtenerAsync<T>(
        string clave,
        CancellationToken cancellationToken = default);

    Task GuardarAsync<T>(
        string clave,
        T valor,
        TimeSpan expiracion,
        CancellationToken cancellationToken = default);

    Task EliminarAsync(
        string clave,
        CancellationToken cancellationToken = default);

    Task<TimeSpan?> ObtenerTiempoVidaAsync(
        string clave,
        CancellationToken cancellationToken = default);
}
