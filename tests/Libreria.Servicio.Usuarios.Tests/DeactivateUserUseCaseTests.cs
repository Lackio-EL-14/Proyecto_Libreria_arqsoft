using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class DeactivateUserUseCaseTests
{
    [Fact]
    public async Task ActiveAdministrator_DeactivatesUser()
    {
        var targetPublicId = Guid.NewGuid();

        var repository = new FakeUserRepository();
        repository.UsersForDeactivation.Add(
            User(
                1,
                Guid.NewGuid(),
                UserStatuses.Active,
                SystemRoles.Administrator));

        repository.UsersForDeactivation.Add(
            User(
                2,
                targetPublicId,
                UserStatuses.Active,
                "Vendedor"));

        var currentUser = new FakeCurrentUser
        {
            UserId = repository.UsersForDeactivation[0].PublicId,
            Username = "admin"
        };

        var transactionRunner = new FakeTransactionRunner();

        var useCase = new DeactivateUserUseCase(
            repository,
            currentUser,
            transactionRunner,
            new ProveedorIdentidadEnMemoria());

        var result =
            await useCase.ExecuteAsync(targetPublicId);

        Assert.Equal(
            DeactivateUserResult.Success,
            result);

        Assert.Equal(2, repository.DeactivatedUserId);
        Assert.Equal(repository.UsersForDeactivation[0].PublicId, repository.DeactivatedByUserId);
        Assert.True(transactionRunner.SerializableExecuted);
    }

    [Fact]
    public async Task NonAdministrator_CannotDeactivateUser()
    {
        var targetPublicId = Guid.NewGuid();

        var repository = new FakeUserRepository();

        repository.UsersForDeactivation.Add(
            User(
                2,
                Guid.NewGuid(),
                UserStatuses.Active,
                "Vendedor"));

        repository.UsersForDeactivation.Add(
            User(
                3,
                targetPublicId,
                UserStatuses.Active,
                "Vendedor"));

        var useCase = CreateUseCase(
            repository,
            currentUserId: 2);

        var result =
            await useCase.ExecuteAsync(targetPublicId);

        Assert.Equal(
            DeactivateUserResult.ActorNotAuthorized,
            result);

        Assert.Null(repository.DeactivatedUserId);
    }

    [Fact]
    public async Task MissingUser_ReturnsNotFound()
    {
        var repository = new FakeUserRepository();

        repository.UsersForDeactivation.Add(
            User(
                1,
                Guid.NewGuid(),
                UserStatuses.Active,
                SystemRoles.Administrator));

        var useCase = CreateUseCase(
            repository,
            currentUserId: 1);

        var result =
            await useCase.ExecuteAsync(Guid.NewGuid());

        Assert.Equal(
            DeactivateUserResult.NotFound,
            result);

        Assert.Null(repository.DeactivatedUserId);
    }

    [Fact]
    public async Task AlreadyInactiveUser_CannotBeDeactivated()
    {
        var targetPublicId = Guid.NewGuid();

        var repository = new FakeUserRepository();

        repository.UsersForDeactivation.Add(
            User(
                1,
                Guid.NewGuid(),
                UserStatuses.Active,
                SystemRoles.Administrator));

        repository.UsersForDeactivation.Add(
            User(
                2,
                targetPublicId,
                UserStatuses.Inactive,
                "Vendedor"));

        var useCase = CreateUseCase(
            repository,
            currentUserId: 1);

        var result =
            await useCase.ExecuteAsync(targetPublicId);

        Assert.Equal(
            DeactivateUserResult.AlreadyInactive,
            result);

        Assert.Null(repository.DeactivatedUserId);
    }

    [Fact]
    public async Task Administrator_CannotDeactivateSelf()
    {
        var actorPublicId = Guid.NewGuid();

        var repository = new FakeUserRepository();

        repository.UsersForDeactivation.Add(
            User(
                1,
                actorPublicId,
                UserStatuses.Active,
                SystemRoles.Administrator));

        repository.UsersForDeactivation.Add(
            User(
                2,
                Guid.NewGuid(),
                UserStatuses.Active,
                SystemRoles.Administrator));

        var useCase = CreateUseCase(
            repository,
            currentUserId: 1);

        var result =
            await useCase.ExecuteAsync(actorPublicId);

        Assert.Equal(
            DeactivateUserResult.SelfDeactivation,
            result);

        Assert.Null(repository.DeactivatedUserId);
    }

    [Fact]
    public async Task LastActiveAdministrator_CannotBeDeactivated()
    {
        var actorPublicId = Guid.NewGuid();

        var repository = new FakeUserRepository();

        repository.UsersForDeactivation.Add(
            User(
                1,
                actorPublicId,
                UserStatuses.Active,
                SystemRoles.Administrator));

        repository.UsersForDeactivation.Add(
            User(
                2,
                Guid.NewGuid(),
                UserStatuses.Active,
                "Vendedor"));

        var useCase = CreateUseCase(
            repository,
            currentUserId: 1);

        var result =
            await useCase.ExecuteAsync(actorPublicId);

        Assert.Equal(
            DeactivateUserResult.LastActiveAdministrator,
            result);

        Assert.Null(repository.DeactivatedUserId);
    }

    [Fact]
    public async Task Baja_DeshabilitaAlUsuarioEnKeycloak()
    {
        var identidad = new ProveedorIdentidadEnMemoria();
        var vendedor = ProveedorIdentidadEnMemoria.IdVendedorSemilla;
        var repository = new FakeUserRepository();
        repository.UsersForDeactivation.Add(User(1, Guid.NewGuid(), UserStatuses.Active, SystemRoles.Administrator));
        repository.UsersForDeactivation.Add(User(2, vendedor, UserStatuses.Active, "Vendedor"));

        var result = await CreateUseCase(repository, currentUserId: 1, identidad).ExecuteAsync(vendedor);

        Assert.Equal(DeactivateUserResult.Success, result);
        Assert.False(identidad.Usuarios[vendedor].Habilitado);
    }

    [Fact]
    public async Task KeycloakCaido_DevuelveNoDisponible()
    {
        var identidad = new ProveedorIdentidadEnMemoria
        {
            FallarSiguienteLlamada = new HttpRequestException("Keycloak caído")
        };
        var target = Guid.NewGuid();
        var repository = new FakeUserRepository();
        repository.UsersForDeactivation.Add(User(1, Guid.NewGuid(), UserStatuses.Active, SystemRoles.Administrator));
        repository.UsersForDeactivation.Add(User(2, target, UserStatuses.Active, "Vendedor"));

        var result = await CreateUseCase(repository, currentUserId: 1, identidad).ExecuteAsync(target);

        Assert.Equal(DeactivateUserResult.IdentityProviderUnavailable, result);
    }

    [Fact]
    public async Task UsuarioSinCuentaEnKeycloak_SeDaDeBajaSoloEnLaBd()
    {
        var identidad = new ProveedorIdentidadEnMemoria { FallarSiguienteLlamada = new HttpRequestException("no debería llamarse") };
        var target = Guid.NewGuid();
        var repository = new FakeUserRepository();
        repository.UsersForDeactivation.Add(User(1, Guid.NewGuid(), UserStatuses.Active, SystemRoles.Administrator));
        repository.UsersForDeactivation.Add(User(2, target, UserStatuses.Active, "Vendedor", sinKeycloak: true));

        var result = await CreateUseCase(repository, currentUserId: 1, identidad).ExecuteAsync(target);

        Assert.Equal(DeactivateUserResult.Success, result);
        Assert.Equal(2, repository.DeactivatedUserId);
    }

    [Fact]
    public async Task ElActorSeIdentificaPorSuSub_NoPorSuPublicId()
    {
        var sub = Guid.NewGuid();
        var target = Guid.NewGuid();
        var repository = new FakeUserRepository();
        repository.UsersForDeactivation.Add(User(1, Guid.NewGuid(), UserStatuses.Active, SystemRoles.Administrator, keycloakId: sub));
        repository.UsersForDeactivation.Add(User(2, target, UserStatuses.Active, "Vendedor"));

        var useCase = new DeactivateUserUseCase(
            repository,
            new FakeCurrentUser { UserId = sub, Username = "admin" },
            new FakeTransactionRunner(),
            new ProveedorIdentidadEnMemoria());

        Assert.Equal(DeactivateUserResult.Success, await useCase.ExecuteAsync(target));
        Assert.Equal(sub, repository.DeactivatedByUserId);
    }

    private static DeactivateUserUseCase CreateUseCase(
        FakeUserRepository repository,
        int currentUserId,
        ProveedorIdentidadEnMemoria? identidad = null)
    {
        return new DeactivateUserUseCase(
            repository,
            new FakeCurrentUser
            {
                UserId = repository.UsersForDeactivation.Single(user => user.UserId == currentUserId).PublicId,
                Username = "usuario-prueba"
            },
            new FakeTransactionRunner(),
            identidad ?? new ProveedorIdentidadEnMemoria());
    }

    private static UserDeactivationData User(
        int userId,
        Guid publicId,
        string status,
        string role,
        bool roleActive = true,
        Guid? keycloakId = null,
        bool sinKeycloak = false)
    {
        // Como en los usuarios semilla, por defecto KeycloakId = PublicId.
        return new UserDeactivationData(
            userId,
            publicId,
            sinKeycloak ? null : keycloakId ?? publicId,
            status,
            role,
            roleActive);
    }
}