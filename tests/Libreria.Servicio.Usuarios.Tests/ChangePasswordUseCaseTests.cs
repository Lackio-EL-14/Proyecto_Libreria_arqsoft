using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class ChangePasswordUseCaseTests
{
    [Fact]
    public async Task AuthenticatedUser_WithValidData_ChangesPassword()
    {
        var repository = new FakeUserRepository
        {
            ActivePasswordHash = "hash-anterior"
        };

        var currentUser = new FakeCurrentUser
        {
            UserId = 10,
            Username = "usuario"
        };

        var hasher = new FakePasswordHasher
        {
            VerifyResult = true,
            GeneratedHash = "hash-nuevo"
        };

        var useCase = CreateUseCase(
            repository,
            currentUser,
            hasher);

        var result = await useCase.ExecuteAsync(
            new ChangePasswordRequest(
                "Actual1!",
                "Nueva123!",
                "Nueva123!"));

        Assert.True(result);
        Assert.Equal(1, repository.UpdatePasswordCalls);
        Assert.Equal(10, repository.UpdatedPasswordUserId);
        Assert.Equal(
            "hash-anterior",
            repository.PreviousPasswordHash);
        Assert.Equal(
            "hash-nuevo",
            repository.NewPasswordHash);
    }

    [Fact]
    public async Task UnauthenticatedUser_CannotChangePassword()
    {
        var repository = new FakeUserRepository();
        var currentUser = new FakeCurrentUser();
        var hasher = new FakePasswordHasher();

        var useCase = CreateUseCase(
            repository,
            currentUser,
            hasher);

        var result = await useCase.ExecuteAsync(
            new ChangePasswordRequest(
                "Actual1!",
                "Nueva123!",
                "Nueva123!"));

        Assert.False(result);
        Assert.Equal(0, repository.UpdatePasswordCalls);
    }

    [Fact]
    public async Task EmptyCurrentPassword_IsRejected()
    {
        var useCase = CreateUseCase(
            new FakeUserRepository(),
            new FakeCurrentUser { UserId = 10 },
            new FakePasswordHasher());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => useCase.ExecuteAsync(
                    new ChangePasswordRequest(
                        string.Empty,
                        "Nueva123!",
                        "Nueva123!")));

        Assert.Equal(
            "Ingrese su contraseña actual.",
            exception.Message);
    }

    [Fact]
    public async Task ConfirmationDifferentFromNewPassword_IsRejected()
    {
        var useCase = CreateUseCase(
            new FakeUserRepository(),
            new FakeCurrentUser { UserId = 10 },
            new FakePasswordHasher());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => useCase.ExecuteAsync(
                    new ChangePasswordRequest(
                        "Actual1!",
                        "Nueva123!",
                        "Otra123!")));

        Assert.Equal(
            "La confirmación no coincide con la nueva contraseña.",
            exception.Message);
    }

    [Fact]
    public async Task WeakNewPassword_IsRejected()
    {
        var useCase = CreateUseCase(
            new FakeUserRepository(),
            new FakeCurrentUser { UserId = 10 },
            new FakePasswordHasher());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => useCase.ExecuteAsync(
                    new ChangePasswordRequest(
                        "Actual1!",
                        "abc",
                        "abc")));

        Assert.Contains(
            "La contraseña debe tener al menos 8 caracteres.",
            exception.Message);
    }

    [Fact]
    public async Task IncorrectCurrentPassword_IsRejected()
    {
        var repository = new FakeUserRepository
        {
            ActivePasswordHash = "hash-anterior"
        };

        var hasher = new FakePasswordHasher
        {
            VerifyResult = false
        };

        var useCase = CreateUseCase(
            repository,
            new FakeCurrentUser { UserId = 10 },
            hasher);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => useCase.ExecuteAsync(
                    new ChangePasswordRequest(
                        "Incorrecta1!",
                        "Nueva123!",
                        "Nueva123!")));

        Assert.Equal(
            "La contraseña actual es incorrecta.",
            exception.Message);

        Assert.Equal(0, repository.UpdatePasswordCalls);
    }

    [Fact]
    public async Task NewPasswordEqualToCurrentPassword_IsRejected()
    {
        var repository = new FakeUserRepository
        {
            ActivePasswordHash = "hash-anterior"
        };

        var hasher = new FakePasswordHasher
        {
            VerifyResult = true
        };

        var useCase = CreateUseCase(
            repository,
            new FakeCurrentUser { UserId = 10 },
            hasher);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => useCase.ExecuteAsync(
                    new ChangePasswordRequest(
                        "Actual123!",
                        "Actual123!",
                        "Actual123!")));

        Assert.Equal(
            "La nueva contraseña debe ser distinta de la actual.",
            exception.Message);

        Assert.Equal(0, repository.UpdatePasswordCalls);
    }

    [Fact]
    public async Task RepositoryCannotUpdate_ThrowsInvalidOperation()
    {
        var repository = new FakeUserRepository
        {
            ActivePasswordHash = "hash-anterior",
            UpdatePasswordResult = false
        };

        var hasher = new FakePasswordHasher
        {
            VerifyResult = true
        };

        var useCase = CreateUseCase(
            repository,
            new FakeCurrentUser { UserId = 10 },
            hasher);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => useCase.ExecuteAsync(
                    new ChangePasswordRequest(
                        "Actual1!",
                        "Nueva123!",
                        "Nueva123!")));

        Assert.Equal(
            "No fue posible cambiar la contraseña. Intente nuevamente.",
            exception.Message);
    }

    private static ChangePasswordUseCase CreateUseCase(
        FakeUserRepository repository,
        FakeCurrentUser currentUser,
        FakePasswordHasher passwordHasher)
    {
        return new ChangePasswordUseCase(
            repository,
            currentUser,
            passwordHasher,
            new PasswordPolicyValidator());
    }
}
