using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class UsernameGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_NormalizaNombreYApellido()
    {
        var repository = new FakeUserRepository();

        var generator =
            new UsernameGenerator(repository);

        var username =
            await generator.GenerateAsync(
                "Álvaro",
                "Núñez");

        Assert.Equal("anunez", username);
    }

    [Fact]
    public async Task GenerateAsync_SiExiste_AgregaSufijoNumerico()
    {
        var repository = new FakeUserRepository();

        repository.ExistingUsernames.Add("jperez");
        repository.ExistingUsernames.Add("jperez2");

        var generator =
            new UsernameGenerator(repository);

        var username =
            await generator.GenerateAsync(
                "Juan",
                "Pérez");

        Assert.Equal("jperez3", username);
    }
}