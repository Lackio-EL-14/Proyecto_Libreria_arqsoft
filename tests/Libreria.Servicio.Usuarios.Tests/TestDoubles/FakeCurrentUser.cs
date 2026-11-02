using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeCurrentUser : ICurrentUser
{
    public int? UserId { get; set; }

    public string? Username { get; set; }
}