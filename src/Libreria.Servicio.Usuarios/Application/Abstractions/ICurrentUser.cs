namespace Libreria.Servicio.Usuarios.Application.Abstractions;

public interface ICurrentUser
{
    int? UserId { get; }
    string? Username { get; }
}