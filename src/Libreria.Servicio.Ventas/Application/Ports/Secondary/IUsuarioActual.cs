namespace Libreria.Servicio.Ventas.Application.Ports.Secondary;

public interface IUsuarioActual
{
    int? UsuarioId { get; }
    string? NombreUsuario { get; }
}
