using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Presentation.Middleware;

namespace Libreria.Servicio.Usuarios.Presentation.Security;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            if (context?.Items.TryGetValue(
                    CurrentUserMiddleware.UserIdItem,
                    out var value) == true &&
                value is int userId)
            {
                return userId;
            }

            return null;
        }
    }

    public string? Username
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            if (context?.Items.TryGetValue(
                    CurrentUserMiddleware.UsernameItem,
                    out var value) == true)
            {
                return value as string;
            }

            return null;
        }
    }
}