using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
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
            new CheckActiveSessionUseCase(repository, new ProveedorIdentidadEnMemoria());

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
            new CheckActiveSessionUseCase(repository, new ProveedorIdentidadEnMemoria());

        var result =
            await useCase.ExecuteAsync(userId);

        Assert.False(result);
    }
    [Fact]
    public async Task ExecuteAsync_PublicIdActivo_DevuelveTrue()
    {
        var publicId = ProveedorIdentidadEnMemoria.IdAdminSemilla;
        var repository = new FakeUserRepository { SessionActive = true };
        var useCase = new CheckActiveSessionUseCase(repository, new ProveedorIdentidadEnMemoria());

        var result = await useCase.ExecuteAsync(publicId);

        Assert.True(result);
        Assert.Equal(publicId, repository.LastSessionPublicId);
    }

    [Fact]
    public async Task ExecuteAsync_PublicIdVacio_DevuelveFalseSinConsultarRepositorio()
    {
        var repository = new FakeUserRepository { SessionActive = true };
        var useCase = new CheckActiveSessionUseCase(repository, new ProveedorIdentidadEnMemoria());

        var result = await useCase.ExecuteAsync(Guid.Empty);

        Assert.False(result);
        Assert.Null(repository.LastSessionPublicId);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task CambioTemporal_ActualizaEstadoAntesDePermitirSesion(bool temporal, bool expected)
    {
        var identity = new ProveedorIdentidadEnMemoria();
        var id = ProveedorIdentidadEnMemoria.IdVendedorSemilla;
        await identity.FijarContrasenaAsync(id, "Prueba123!", temporal);
        var user = new Libreria.Servicio.Usuarios.Domain.Entities.User { KeycloakId = id, Status = "Pendiente" };
        var repository = new FakeUserRepository { SessionActive = true, UserByPublicId = user };
        var useCase = new CheckActiveSessionUseCase(repository, identity);
        Assert.Equal(expected, await useCase.ExecuteAsync(id));
        Assert.Equal(expected ? "Activo" : "Pendiente", user.Status);
        Assert.Equal(expected ? id : (Guid?)null, repository.ActivatedKeycloakId);
    }

    [Fact]
    public async Task Baja_NoConsultaKeycloakNiActiva()
    {
        var identity = new ProveedorIdentidadEnMemoria { FallarSiguienteLlamada = new Exception("No debe consultarse") };
        var repository = new FakeUserRepository { SessionActive = false };
        Assert.False(await new CheckActiveSessionUseCase(repository, identity).ExecuteAsync(Guid.NewGuid()));
        Assert.Null(repository.ActivatedKeycloakId);
    }

    [Fact]
    public async Task UsuarioNoEncontradoEnKeycloak_NoActiva()
    {
        var repository = new FakeUserRepository { SessionActive = true };
        var useCase = new CheckActiveSessionUseCase(repository, new ProveedorIdentidadEnMemoria());
        Assert.False(await useCase.ExecuteAsync(Guid.NewGuid()));
        Assert.Null(repository.ActivatedKeycloakId);
    }
}