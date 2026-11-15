namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IUsuarioActual
{
    Guid? UsuarioId { get; }
    string? NombreUsuario { get; }
    string? Iniciales { get; }
}
