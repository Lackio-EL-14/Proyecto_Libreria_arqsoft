using System.Globalization;

namespace Libreria.Servicio.Reportes.Presentation.Middleware;

public sealed class CurrentUserMiddleware(RequestDelegate next)
{
    public const string UserIdItem = "CurrentUser.UserId";
    public const string UsernameItem = "CurrentUser.Username";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/reportes"))
        {
            await next(context);
            return;
        }
        var ids = context.Request.Headers["X-Usuario-Id"];
        var names = context.Request.Headers["X-Usuario-Nombre"];
        if (ids.Count == 0 || names.Count == 0)
        {
            await Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
                title: "Se requiere el usuario de la sesión.").ExecuteAsync(context);
            return;
        }
        if (ids.Count != 1 || names.Count != 1 ||
            !int.TryParse(ids[0], NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0 ||
            string.IsNullOrWhiteSpace(names[0]) || names[0]!.Length > 50 || names[0]!.Any(char.IsControl))
        {
            await Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Los encabezados del usuario no son válidos.").ExecuteAsync(context);
            return;
        }
        context.Items[UserIdItem] = id;
        context.Items[UsernameItem] = names[0]!;
        await next(context);
    }
}
