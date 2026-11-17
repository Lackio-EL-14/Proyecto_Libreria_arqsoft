namespace Libreria.Servicio.Proveedores.Application.Ports.Secondary;

public interface IUsuarioActual { Guid? UsuarioId { get; } string? NombreUsuario { get; } string? Iniciales { get; } }
