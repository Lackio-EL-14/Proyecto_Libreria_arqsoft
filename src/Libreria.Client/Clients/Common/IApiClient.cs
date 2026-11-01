namespace Libreria.Client.Clients.Common;

public interface IApiClient
{
    Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);
    Task PostAsync<TRequest>(string requestUri, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default);
    Task PutAsync<TRequest>(string requestUri, TRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default);
}
