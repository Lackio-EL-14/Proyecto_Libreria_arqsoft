using System.Globalization;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Microsoft.AspNetCore.Http;

namespace Libreria.Servicio.Ventas.Infrastructure.Security;

// Adaptador del puerto IUsuarioActual para el modelo SBA.
// En el monolito el usuario salía de los claims de la cookie; ahora el servicio
// no tiene sesión propia: el Front le informa quién opera mediante el
// encabezado X-Usuario-Id. El núcleo (Application) no cambia en absoluto.
public class UsuarioActualDesdeEncabezados : IUsuarioActual
{
    public const string EncabezadoUsuarioId = "X-Usuario-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActualDesdeEncabezados(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UsuarioId
    {
        get
        {
            var valor = _httpContextAccessor.HttpContext?
                .Request.Headers[EncabezadoUsuarioId].ToString();

            return int.TryParse(valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var usuarioId)
                   && usuarioId > 0
                ? usuarioId
                : null;
        }
    }
}
