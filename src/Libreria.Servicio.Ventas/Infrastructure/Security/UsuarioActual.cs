using System.Globalization;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Microsoft.AspNetCore.Http;

namespace Libreria.Servicio.Ventas.Infrastructure.Security;

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
