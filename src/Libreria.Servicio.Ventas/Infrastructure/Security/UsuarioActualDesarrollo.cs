using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Infrastructure.Security;

public sealed class UsuarioActualDesarrollo : IUsuarioActual
{
    public const string Seccion = "UsuarioDesarrollo";

    public UsuarioActualDesarrollo(IConfiguration configuracion)
    {
        var seccion = configuracion.GetSection(Seccion);

        UsuarioId = Guid.TryParse(seccion["PublicId"], out var publicId)
            ? publicId
            : null;
        NombreUsuario = seccion["Nombre"];
        Iniciales = seccion["Iniciales"];
    }

    public Guid? UsuarioId { get; }
    public string? NombreUsuario { get; }
    public string? Iniciales { get; }
}
