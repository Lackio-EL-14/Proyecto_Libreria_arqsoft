using System.Net.Http.Json;
using System.Text.Json;
using Libreria.Client.DTOs.Common;

namespace Libreria.Client.Clients.Common;

public abstract class ApiClientBase : IApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly string _servicio;

    protected ApiClientBase(HttpClient httpClient, string servicio)
    {
        _httpClient = httpClient;
        _servicio = servicio;
    }

    public Task<TResponse?> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(new HttpRequestMessage(HttpMethod.Get, requestUri), cancellationToken);

    public Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(CreateJsonRequest(HttpMethod.Post, requestUri, request), cancellationToken);

    public Task PostAsync<TRequest>(string requestUri, TRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(CreateJsonRequest(HttpMethod.Post, requestUri, request), cancellationToken);

    public Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(CreateJsonRequest(HttpMethod.Put, requestUri, request), cancellationToken);

    public Task PutAsync<TRequest>(string requestUri, TRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(CreateJsonRequest(HttpMethod.Put, requestUri, request), cancellationToken);

    public Task DeleteAsync(string requestUri, CancellationToken cancellationToken = default) =>
        SendAsync(new HttpRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);

    private static HttpRequestMessage CreateJsonRequest<TRequest>(HttpMethod method, string requestUri, TRequest request) =>
        new(method, requestUri) { Content = JsonContent.Create(request, options: JsonOptions) };

    private async Task<TResponse?> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        if (response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
    }

    private async Task SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await SendCoreAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ServicioNoDisponibleException(_servicio, exception);
        }
        catch (HttpRequestException exception)
        {
            throw new ServicioNoDisponibleException(_servicio, exception);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        ApiProblemDetailsDto? problem = null;

        try
        {
            problem = await response.Content.ReadFromJsonAsync<ApiProblemDetailsDto>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
        }

        var message = problem?.Detail ?? problem?.Title ?? $"El servicio de {_servicio} rechazó la solicitud.";
        throw new ApiClientException(_servicio, response.StatusCode, message);
    }
}
