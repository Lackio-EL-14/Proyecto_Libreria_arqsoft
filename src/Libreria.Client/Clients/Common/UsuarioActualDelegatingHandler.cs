using System.Security.Claims;

namespace Libreria.Client.Clients.Common;

public sealed class UsuarioActualDelegatingHandler : DelegatingHandler
{
    public const string UsuarioIdHeader = "X-Usuario-Id";
    public const string UsuarioNombreHeader = "X-Usuario-Nombre";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated == true)
        {
            var usuarioId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var usuarioNombre = principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.Identity.Name;

            if (!int.TryParse(usuarioId, out var id) ||
                id <= 0 ||
                string.IsNullOrWhiteSpace(usuarioNombre))
            {
                throw new InvalidOperationException(
                    "La sesión autenticada no contiene los claims requeridos para la auditoría.");
            }

            AddHeader(request, UsuarioIdHeader, id.ToString());
            AddHeader(request, UsuarioNombreHeader, usuarioNombre);
        }

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
