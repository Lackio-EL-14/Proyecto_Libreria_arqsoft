using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeApplicationConfiguration : IApplicationConfiguration
{
    public string BaseUrl { get; set; } = "https://localhost:5000";
}
