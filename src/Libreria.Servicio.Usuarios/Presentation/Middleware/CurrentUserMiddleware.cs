
namespace Libreria.Servicio.Usuarios.Presentation.Middleware;

// TRANSITORIO hasta US-126/127: confía en X-Usuario-Id (= sub de Keycloak) y X-Usuario-Nombre
// (= preferred_username) que envía el Front junto al Bearer, sin validar el token. Lo reemplaza el
// usuario actual leído del JWT validado. No exponer el puerto 5102 fuera de localhost.
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