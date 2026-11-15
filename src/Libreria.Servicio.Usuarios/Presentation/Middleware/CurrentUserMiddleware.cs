using System.Globalization;

namespace Libreria.Servicio.Usuarios.Presentation.Middleware;

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

        if (int.TryParse(
                usuarioId,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var userId))
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