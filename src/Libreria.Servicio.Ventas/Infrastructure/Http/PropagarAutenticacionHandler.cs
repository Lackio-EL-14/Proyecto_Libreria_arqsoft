using System.Net.Http.Headers;

namespace Libreria.Servicio.Ventas.Infrastructure.Http;

/// <summary>
/// Propaga la identidad recibida por Ventas a sus llamadas directas a otros MS.
/// Acepta el JWT real y conserva temporalmente los encabezados de desarrollo.
/// </summary>
public sealed class PropagarAutenticacionHandler : DelegatingHandler
{
    private static readonly string[] EncabezadosDesarrollo =
    [
        "X-Usuario-Id",
        "X-Usuario-Nombre",
        "X-Usuario-Iniciales"
    ];

    private readonly IHttpContextAccessor _httpContextAccessor;

    public PropagarAutenticacionHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var entrada = _httpContextAccessor.HttpContext?.Request;
        var authorization = entrada?.Headers.Authorization.ToString();

        if (!string.IsNullOrWhiteSpace(authorization)
            && AuthenticationHeaderValue.TryParse(authorization, out var autorizacion))
        {
            request.Headers.Authorization = autorizacion;
        }

        foreach (var nombre in EncabezadosDesarrollo)
        {
            var valor = entrada?.Headers[nombre].ToString();
            if (string.IsNullOrWhiteSpace(valor))
            {
                continue;
            }

            request.Headers.Remove(nombre);
            request.Headers.TryAddWithoutValidation(
                nombre,
                valor.Replace("\r", string.Empty).Replace("\n", string.Empty));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
