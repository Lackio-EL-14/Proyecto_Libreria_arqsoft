using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class GetUserNamesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_EliminaIdsInvalidosYDuplicados()
    {
        var repository = new FakeUserRepository();

        repository.Names[1] = "Ana Pérez";
        repository.Names[2] = "Luis Gómez";

        var factory =
            new FakeUsuarioRepositoryFactory(repository);

        var useCase =
            new GetUserNamesUseCase(factory);

        var result = await useCase.ExecuteAsync(
            new[] { 1, 2, 2, 0, -1 });

        Assert.Equal(2, result.Count);
        Assert.Equal("Ana Pérez", result[1]);
        Assert.Equal("Luis Gómez", result[2]);
    }
}