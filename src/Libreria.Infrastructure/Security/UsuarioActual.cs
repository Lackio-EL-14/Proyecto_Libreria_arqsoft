using System.Security.Claims;
using Libreria.Application.Ports.Secondary;
using Microsoft.AspNetCore.Http;

namespace Libreria.Infrastructure.Security;

public sealed class UsuarioActual : IUsuarioActual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActual(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UsuarioId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(value, out var usuarioId) ? usuarioId : null;
        }
    }
}
