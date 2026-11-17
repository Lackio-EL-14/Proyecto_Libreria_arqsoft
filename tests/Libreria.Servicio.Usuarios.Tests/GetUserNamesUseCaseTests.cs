using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class GetUserNamesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_GuidPublico_DevuelveNombre()
    {
        var publicId = Guid.NewGuid();
        var repository = new FakeUserRepository();
        repository.PublicNames[publicId] = "Juan Pérez";
        var useCase = new GetUserNamesUseCase(
            new FakeUsuarioRepositoryFactory(repository));

        var result = await useCase.ExecuteAsync(new[] { publicId });

        Assert.Single(result);
        Assert.Equal("Juan Pérez", result[publicId]);
    }

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