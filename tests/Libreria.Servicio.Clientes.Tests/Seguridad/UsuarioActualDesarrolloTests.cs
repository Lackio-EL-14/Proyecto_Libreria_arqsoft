using Libreria.Servicio.Clientes.Infrastructure.Seguridad;
using Microsoft.Extensions.Configuration;

namespace Libreria.Servicio.Clientes.Tests.Seguridad;

public class UsuarioActualDesarrolloTests
{
    [Fact]
    public void Lee_el_usuario_de_la_configuracion()
    {
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UsuarioDesarrollo:PublicId"] = "a0000000-0000-0000-0000-000000000001",
                ["UsuarioDesarrollo:Nombre"] = "admin",
                ["UsuarioDesarrollo:Iniciales"] = "ADM"
            })
            .Build();

        var usuario = new UsuarioActualDesarrollo(configuracion);

        Assert.Equal(Guid.Parse("a0000000-0000-0000-0000-000000000001"), usuario.UsuarioId);
        Assert.Equal("admin", usuario.NombreUsuario);
        Assert.Equal("ADM", usuario.Iniciales);
    }

    [Fact]
    public void Sin_configuracion_no_hay_usuario()
    {
        var usuario = new UsuarioActualDesarrollo(new ConfigurationBuilder().Build());

        Assert.Null(usuario.UsuarioId);
    }
}
