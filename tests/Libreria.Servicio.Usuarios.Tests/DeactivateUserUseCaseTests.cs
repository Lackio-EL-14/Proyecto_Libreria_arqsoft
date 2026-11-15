using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;
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
            UserId = 1,
            Username = "admin"
        };

        var transactionRunner = new FakeTransactionRunner();

        var useCase = new DeactivateUserUseCase(
            repository,
            currentUser,
            transactionRunner);

        var result =
            await useCase.ExecuteAsync(targetPublicId);

        Assert.Equal(
            DeactivateUserResult.Success,
            result);

        Assert.Equal(2, repository.DeactivatedUserId);
        Assert.Equal(1, repository.DeactivatedByUserId);
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

    private static DeactivateUserUseCase CreateUseCase(
        FakeUserRepository repository,
        int currentUserId)
    {
        return new DeactivateUserUseCase(
            repository,
            new FakeCurrentUser
            {
                UserId = currentUserId,
                Username = "usuario-prueba"
            },
            new FakeTransactionRunner());
    }

    private static UserDeactivationData User(
        int userId,
        Guid publicId,
        string status,
        string role,
        bool roleActive = true)
    {
        return new UserDeactivationData(
            userId,
            publicId,
            status,
            role,
            roleActive);
    }
}