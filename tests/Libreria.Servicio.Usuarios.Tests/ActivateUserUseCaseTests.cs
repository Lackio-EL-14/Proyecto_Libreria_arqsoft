using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class ActivateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_TokenValido_ActivaUsuario()
    {
        const string token = "token-seguro-de-prueba";

        var repository = new FakeUserRepository
        {
            ActivationUser =
                CreatePendingUser(token)
        };

        var passwordHasher =
            new FakePasswordHasher
            {
                GeneratedHash = "hash-argon2-prueba"
            };

        var useCase = new ActivateUserUseCase(
            repository,
            passwordHasher,
            new PasswordPolicyValidator());

        await useCase.ExecuteAsync(
            new ActivateUserRequest(
                repository.ActivationUser!
                    .ActivationTokenHash!,
                token,
                "Segura1!",
                "Segura1!"));

        Assert.Equal(
            repository.ActivationUser.Id,
            repository.ActivatedUserId);

        Assert.Equal(
            "hash-argon2-prueba",
            repository.ActivatedPasswordHash);

        Assert.NotNull(repository.ActivatedAt);
    }

    [Fact]
    public async Task ExecuteAsync_TokenIncorrecto_LanzaMensajeGenerico()
    {
        var repository = new FakeUserRepository
        {
            ActivationUser =
                CreatePendingUser(
                    "token-correcto")
        };

        var useCase = CreateUseCase(repository);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () => useCase.ExecuteAsync(
                    new ActivateUserRequest(
                        repository.ActivationUser!
                            .ActivationTokenHash!,
                        "token-incorrecto",
                        "Segura1!",
                        "Segura1!")));

        Assert.Equal(
            "El enlace de activación no es válido o ha expirado.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TokenVencido_LanzaMensajeGenerico()
    {
        var user =
            CreatePendingUser("token");

        user.ActivationTokenExpiresAt =
            DateTime.UtcNow.AddMinutes(-1);

        var repository =
            new FakeUserRepository
            {
                ActivationUser = user
            };

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () => CreateUseCase(repository)
                    .ExecuteAsync(
                        new ActivateUserRequest(
                            user.ActivationTokenHash!,
                            "token",
                            "Segura1!",
                            "Segura1!")));

        Assert.Equal(
            "El enlace de activación no es válido o ha expirado.",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_TokenYaUsado_LanzaMensajeGenerico()
    {
        var user =
            CreatePendingUser("token");

        user.ActivationTokenUsedAt =
            DateTime.UtcNow.AddMinutes(-5);

        var repository =
            new FakeUserRepository
            {
                ActivationUser = user
            };

        await Assert.ThrowsAsync<
            InvalidOperationException>(
            () => CreateUseCase(repository)
                .ExecuteAsync(
                    new ActivateUserRequest(
                        user.ActivationTokenHash!,
                        "token",
                        "Segura1!",
                        "Segura1!")));
    }

    [Fact]
    public async Task ExecuteAsync_ConfirmacionDistinta_LanzaError()
    {
        var user =
            CreatePendingUser("token");

        var repository =
            new FakeUserRepository
            {
                ActivationUser = user
            };

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () => CreateUseCase(repository)
                    .ExecuteAsync(
                        new ActivateUserRequest(
                            user.ActivationTokenHash!,
                            "token",
                            "Segura1!",
                            "Diferente1!")));

        Assert.Contains(
            "no coinciden",
            exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ContrasenaInsegura_LanzaError()
    {
        var user =
            CreatePendingUser("token");

        var repository =
            new FakeUserRepository
            {
                ActivationUser = user
            };

        var exception =
            await Assert.ThrowsAsync<
                ArgumentException>(
                () => CreateUseCase(repository)
                    .ExecuteAsync(
                        new ActivateUserRequest(
                            user.ActivationTokenHash!,
                            "token",
                            "abc",
                            "abc")));

        Assert.Contains(
            "al menos 8 caracteres",
            exception.Message);

        Assert.Null(repository.ActivatedUserId);
    }

    private static ActivateUserUseCase CreateUseCase(
        FakeUserRepository repository)
    {
        return new ActivateUserUseCase(
            repository,
            new FakePasswordHasher(),
            new PasswordPolicyValidator());
    }

    private static User CreatePendingUser(
        string token)
    {
        return new User
        {
            Id = 10,
            PublicId = Guid.NewGuid(),
            FirstName = "Juan",
            LastName = "Pérez",
            FullName = "Juan Pérez",
            Email = "juan@example.com",
            Username = "jperez",
            RoleId = 2,
            Status = UserStatuses.Pending,
            ActivationTokenHash =
                ActivationTokenService
                    .CalculateHash(token),
            ActivationTokenExpiresAt =
                DateTime.UtcNow.AddHours(1),
            ActivationTokenUsedAt = null
        };
    }
}