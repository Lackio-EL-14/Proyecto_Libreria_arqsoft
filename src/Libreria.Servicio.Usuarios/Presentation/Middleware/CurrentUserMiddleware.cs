
namespace Libreria.Servicio.Usuarios.Presentation.Middleware;

// TRANSITORIO: confía en X-Usuario-Id/X-Usuario-Nombre sin autenticar. Lo reemplaza el
// usuario actual desde el token de Keycloak (US-126 y US-127). No exponer el puerto 5102 fuera de localhost.
public sealed class CurrentUserMiddleware
{
    public const string UserIdItem = "CurrentUser.UserId";
    public const string UsernameItem = "CurrentUser.Username";

    private const string UsuarioIdHeader = "X-Usuario-Id";
    private const string UsuarioNombreHeader = "X-Usuario-Nombre";

    private readonly RequestDelegate _next;

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var usuarioId = context.Request.Headers[UsuarioIdHeader]
            .FirstOrDefault();

        if (Guid.TryParse(usuarioId, out var userId))
        {
            context.Items[UserIdItem] = userId;
        }

        var username = context.Request.Headers[UsuarioNombreHeader]
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(username))
        {
            context.Items[UsernameItem] = username;
        }

        await _next(context);
    }
}