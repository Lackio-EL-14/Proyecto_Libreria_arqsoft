using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Libreria.Client.Clients.Common;

public sealed class UsuarioActualDelegatingHandler : DelegatingHandler
{
    public const string UsuarioIdHeader = "X-Usuario-Id";
    public const string UsuarioNombreHeader = "X-Usuario-Nombre";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _environment;
    private readonly UsuarioDesarrolloOptions _usuarioDesarrollo;

    public UsuarioActualDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment environment,
        IOptions<UsuarioDesarrolloOptions> usuarioDesarrollo)
    {
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _usuarioDesarrollo = usuarioDesarrollo.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var usuarioId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var usuarioNombre = principal?.FindFirstValue(ClaimTypes.Name) ?? principal?.Identity?.Name;

        if ((!principal?.Identity?.IsAuthenticated ?? true) && _environment.IsDevelopment())
        {
            usuarioId = _usuarioDesarrollo.Id;
            usuarioNombre = _usuarioDesarrollo.Nombre;
        }

        AddHeader(request, UsuarioIdHeader, usuarioId);
        AddHeader(request, UsuarioNombreHeader, usuarioNombre);

        return base.SendAsync(request, cancellationToken);
    }

    private static void AddHeader(HttpRequestMessage request, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        request.Headers.Remove(name);
        request.Headers.TryAddWithoutValidation(name, value.Replace("\r", string.Empty).Replace("\n", string.Empty));
    }
}
