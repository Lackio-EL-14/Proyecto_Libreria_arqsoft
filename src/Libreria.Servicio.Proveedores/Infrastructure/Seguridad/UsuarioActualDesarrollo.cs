using Libreria.Servicio.Proveedores.Application.Ports.Secondary;

namespace Libreria.Servicio.Proveedores.Infrastructure.Seguridad;

public sealed class UsuarioActualDesarrollo : IUsuarioActual
{
    public UsuarioActualDesarrollo(IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection("UsuarioDesarrollo");
        UsuarioId = Guid.TryParse(seccion["PublicId"], out var id) ? id : null;
        NombreUsuario = seccion["Nombre"];
        Iniciales = seccion["Iniciales"];
    }
    public Guid? UsuarioId { get; }
    public string? NombreUsuario { get; }
    public string? Iniciales { get; }
}
