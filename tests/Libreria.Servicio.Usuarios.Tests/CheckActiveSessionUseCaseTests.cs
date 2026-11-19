using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class CheckActiveSessionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_UsuarioActivo_DevuelveTrue()
    {
        var repository = new FakeUserRepository
        {
            SessionActive = true
        };

        var useCase =
            new CheckActiveSessionUseCase(repository);

        var result = await useCase.ExecuteAsync(5);

        Assert.True(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteAsync_IdInvalido_DevuelveFalse(
        int userId)
    {
        var repository = new FakeUserRepository
        {
            SessionActive = true
        };

        var useCase =
            new CheckActiveSessionUseCase(repository);

        var result =
            await useCase.ExecuteAsync(userId);

        Assert.False(result);
    }
    [Fact]
    public async Task ExecuteAsync_PublicIdActivo_DevuelveTrue()
    {
        var publicId = Guid.NewGuid();
        var repository = new FakeUserRepository { SessionActive = true };
        var useCase = new CheckActiveSessionUseCase(repository);

        var result = await useCase.ExecuteAsync(publicId);

        Assert.True(result);
        Assert.Equal(publicId, repository.LastSessionPublicId);
    }

    [Fact]
    public async Task ExecuteAsync_PublicIdVacio_DevuelveFalseSinConsultarRepositorio()
    {
        var repository = new FakeUserRepository { SessionActive = true };
        var useCase = new CheckActiveSessionUseCase(repository);

        var result = await useCase.ExecuteAsync(Guid.Empty);

        Assert.False(result);
        Assert.Null(repository.LastSessionPublicId);
    }
}