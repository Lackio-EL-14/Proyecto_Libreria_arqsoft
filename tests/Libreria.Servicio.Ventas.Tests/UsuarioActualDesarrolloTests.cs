using Libreria.Servicio.Ventas.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace Libreria.Servicio.Ventas.Tests;

public class UsuarioActualDesarrolloTests
{
    [Fact]
    public void Construir_LeeElContratoGuidNombreEIniciales()
    {
        var valores = new Dictionary<string, string?>
        {
            ["UsuarioDesarrollo:PublicId"] =
                "a0000000-0000-0000-0000-000000000001",
            ["UsuarioDesarrollo:Nombre"] = "admin",
            ["UsuarioDesarrollo:Iniciales"] = "A.S."
        };
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();

        var usuario = new UsuarioActualDesarrollo(configuracion);

        Assert.Equal(
            Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            usuario.UsuarioId);
        Assert.Equal("admin", usuario.NombreUsuario);
        Assert.Equal("A.S.", usuario.Iniciales);
    }

    [Fact]
    public void Construir_ConGuidInvalido_NoInventaUnUsuario()
    {
        var valores = new Dictionary<string, string?>
        {
            ["UsuarioDesarrollo:PublicId"] = "invalido"
        };
        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(valores)
            .Build();

        var usuario = new UsuarioActualDesarrollo(configuracion);

        Assert.Null(usuario.UsuarioId);
    }
}
