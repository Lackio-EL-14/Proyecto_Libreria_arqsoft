namespace Libreria.Servicio.Reportes.Application.Abstractions;

public interface ICurrentUser
{
    int? UserId { get; }
    string? Username { get; }
}
