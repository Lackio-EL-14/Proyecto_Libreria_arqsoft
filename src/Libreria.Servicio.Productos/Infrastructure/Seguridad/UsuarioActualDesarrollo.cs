using Libreria.Servicio.Productos.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Libreria.Servicio.Productos.Infrastructure.Seguridad;

/// <summary>
/// Adaptador temporal del puerto <see cref="IUsuarioActual"/>: devuelve el usuario `admin`
/// de los datos semilla. Solo se registra en Development; lo reemplaza
/// UsuarioActualDesdeToken cuando el JWT esté disponible (US-127).
/// </summary>
public sealed class UsuarioActualDesarrollo : IUsuarioActual
{
    public const string Seccion = "UsuarioDesarrollo";

    public UsuarioActualDesarrollo(IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection(Seccion);

        UsuarioId = Guid.TryParse(seccion["PublicId"], out var publicId) ? publicId : null;
        NombreUsuario = seccion["Nombre"];
        Iniciales = seccion["Iniciales"];
    }

    public Guid? UsuarioId { get; }
    public string? NombreUsuario { get; }
    public string? Iniciales { get; }
}
