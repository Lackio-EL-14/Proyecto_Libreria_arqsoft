using System.Globalization;
using System.Linq;
using Libreria.Application.Ports.Secondary;
using Microsoft.AspNetCore.Http;

namespace Libreria.Infrastructure.Security;

// TEMPORAL (puente): lee el usuario desde el header HTTP para el módulo
// de Usuarios que aún vive en el Front. Se elimina con US-92/US-93.
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
            var valor = _httpContextAccessor.HttpContext?.Request
                .Headers["X-Usuario-Id"]
                .FirstOrDefault();

            return int.TryParse(
                    valor,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var usuarioId) &&
                usuarioId > 0
                ? usuarioId
                : null;
        }
    }
}
