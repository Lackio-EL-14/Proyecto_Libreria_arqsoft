using System.Net;
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

    // Operaciones tipadas que devuelven ResultadoApi (estilo que usan las páginas de
    // inventario y de ventas). Las escrituras nunca lanzan; las lecturas lanzan
    // ServicioNoDisponibleException si el MS o el gateway no responden (502/503/504 o red)
    // y HttpRequestException ante cualquier otro error.

    /// <summary>GET que exige 2xx.</summary>
    protected async Task<T?> LeerAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var respuesta = await SendCoreAsync(new HttpRequestMessage(HttpMethod.Get, url), ct);
        AsegurarLectura(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    /// <summary>GET que devuelve <c>null</c> si el recurso no existe (404).</summary>
    protected async Task<T?> LeerOpcionalAsync<T>(string url, CancellationToken ct) where T : class
    {
        using var respuesta = await SendCoreAsync(new HttpRequestMessage(HttpMethod.Get, url), ct);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        AsegurarLectura(respuesta);
        return await respuesta.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
    }

    private void AsegurarLectura(HttpResponseMessage respuesta)
    {
        if (EsServicioNoDisponible(respuesta.StatusCode))
        {
            throw CrearServicioNoDisponible(respuesta.StatusCode);
        }

        respuesta.EnsureSuccessStatusCode();
    }

    protected Task<ResultadoApi> EnviarPostAsync<TCuerpo>(string url, TCuerpo cuerpo, CancellationToken ct) =>
        EnviarAsync(() => _httpClient.PostAsJsonAsync(url, cuerpo, ct), ct);

    protected Task<ResultadoApi> EnviarPutAsync<TCuerpo>(string url, TCuerpo cuerpo, CancellationToken ct) =>
        EnviarAsync(() => _httpClient.PutAsJsonAsync(url, cuerpo, ct), ct);

    protected Task<ResultadoApi> EnviarPatchAsync(string url, CancellationToken ct) =>
        EnviarAsync(() => _httpClient.PatchAsync(url, null, ct), ct);

    private async Task<ResultadoApi> EnviarAsync(Func<Task<HttpResponseMessage>> peticion, CancellationToken ct)
    {
        try
        {
            using var respuesta = await peticion();
            return await ResultadoApiTraductor.ConvertirAsync(respuesta, _servicio, ct);
        }
        catch (HttpRequestException)
        {
            return ResultadoApi.Invalido(
                $"No se pudo conectar con el servicio de {_servicio}. Verifique que esté en ejecución.");
        }
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

        // Detrás del gateway, un microservicio caído llega como 502/503/504 y no como error de red.
        if (EsServicioNoDisponible(response.StatusCode))
        {
            throw CrearServicioNoDisponible(response.StatusCode);
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
        throw new ApiClientException(
            _servicio,
            response.StatusCode,
            message,
            problem?.Errors);
    }

    private ServicioNoDisponibleException CrearServicioNoDisponible(HttpStatusCode statusCode) =>
        new(_servicio, new HttpRequestException($"El gateway respondió {(int)statusCode}.", null, statusCode));

    private static bool EsServicioNoDisponible(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
