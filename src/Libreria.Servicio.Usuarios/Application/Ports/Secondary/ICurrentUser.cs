namespace Libreria.Servicio.Usuarios.Application.Ports.Secondary;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Username { get; }
}