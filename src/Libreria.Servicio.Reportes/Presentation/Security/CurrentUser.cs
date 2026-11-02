using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Presentation.Middleware;

namespace Libreria.Servicio.Reportes.Presentation.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int? UserId => accessor.HttpContext?.Items[CurrentUserMiddleware.UserIdItem] as int?;
    public string? Username => accessor.HttpContext?.Items[CurrentUserMiddleware.UsernameItem] as string;
}
