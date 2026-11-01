using System.Security.Claims;
using Libreria.Application.Ports.Secondary;
using Microsoft.AspNetCore.Http;

namespace Libreria.Infrastructure.Security;

// TEMPORAL (puente): lee el usuario desde los claims de la cookie para el módulo
// de Usuarios que aún vive en el Front. Se elimina con US-92/US-93.
public class UsuarioActual : IUsuarioActual
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
            var valor = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(valor, out var usuarioId)
                ? usuarioId
                : null;
        }
    }

}
