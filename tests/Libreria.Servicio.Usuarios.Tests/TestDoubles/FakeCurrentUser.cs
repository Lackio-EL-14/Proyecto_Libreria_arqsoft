using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }

    public string? Username { get; set; }
}