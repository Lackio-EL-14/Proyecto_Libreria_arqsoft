using System.Globalization;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Microsoft.AspNetCore.Http;

namespace Libreria.Servicio.Ventas.Infrastructure.Security;

public class UsuarioActualDesdeEncabezados : IUsuarioActual
{
    public const string EncabezadoUsuarioId = "X-Usuario-Id";
    public const string EncabezadoUsuarioNombre = "X-Usuario-Nombre";

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

    public string? NombreUsuario
    {
        get
        {
            var valor = _httpContextAccessor.HttpContext?
                .Request.Headers[EncabezadoUsuarioNombre].ToString();

            return string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        }
    }
}
