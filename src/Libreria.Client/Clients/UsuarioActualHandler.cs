using System.Security.Claims;

namespace Libreria.Client.Clients;

// Agrega a TODA llamada hacia un servicio los encabezados del usuario en sesión
// (contrato entre grupos, sección 6 del planning). Así cada servicio llena sus
// atributos de auditoría sin tener sesión propia.
// Nota: Cesar (US-80) puede ampliar este handler; el contrato de encabezados no cambia.
public class UsuarioActualHandler : DelegatingHandler
{
    public const string EncabezadoUsuarioId = "X-Usuario-Id";
    public const string EncabezadoUsuarioNombre = "X-Usuario-Nombre";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var usuario = _httpContextAccessor.HttpContext?.User;
        var usuarioId = usuario?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var nombre = usuario?.Identity?.Name;

        request.Headers.Remove(EncabezadoUsuarioId);
        request.Headers.Remove(EncabezadoUsuarioNombre);

        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            request.Headers.TryAddWithoutValidation(EncabezadoUsuarioId, usuarioId);
        }

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            request.Headers.TryAddWithoutValidation(
                EncabezadoUsuarioNombre, Uri.EscapeDataString(nombre));
        }

        return base.SendAsync(request, cancellationToken);
    }
}
