using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class CreateUserUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_Admin_CreaUsuarioPendienteYEnviaCorreo()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = true
        };

        repository.ActiveRoles["Vendedor"] = 2;

        var currentUser = new FakeCurrentUser
        {
            UserId = 1,
            Username = "admin"
        };

        var emailSender = new FakeEmailSender();

        var configuration =
            new FakeApplicationConfiguration
            {
                BaseUrl = "http://localhost:5000"
            };

        var usernameGenerator =
            new UsernameGenerator(repository);

        var facade = new UsuarioFacade(
            usernameGenerator,
            emailSender,
            configuration);

        var useCase = new CreateUserUseCase(
            repository,
            currentUser,
            facade,
            new FakeTransactionRunner());

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest(
                "José",
                "Pérez",
                "jose@example.com",
                "Vendedor"));

        Assert.NotNull(result);
        Assert.Equal("jperez", result.Username);

        Assert.NotNull(repository.CreatedUser);
        Assert.Equal(
            UserStatuses.Pending,
            repository.CreatedUser.Status);

        Assert.Equal(
            "jose@example.com",
            repository.CreatedUser.Email);

        Assert.Equal(
            1,
            repository.CreatedUser.CreatedByUserId);

        Assert.False(
            string.IsNullOrWhiteSpace(
                repository.CreatedUser
                    .ActivationTokenHash));

        Assert.NotNull(
            repository.CreatedUser
                .ActivationTokenExpiresAt);

        Assert.Null(
            repository.CreatedUser
                .ActivationTokenUsedAt);

        Assert.Equal(
            "jose@example.com",
            emailSender.Recipient);

        Assert.Contains(
            "http://localhost:5000/Usuarios/Activar",
            emailSender.HtmlBody);

        Assert.Contains(
            "jperez",
            emailSender.HtmlBody);
    }

    [Fact]
    public async Task ExecuteAsync_NoAdministrador_NoCreaUsuario()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = false
        };

        var currentUser = new FakeCurrentUser
        {
            UserId = 2,
            Username = "vendedor"
        };

        var facade = new UsuarioFacade(
            new UsernameGenerator(repository),
            new FakeEmailSender(),
            new FakeApplicationConfiguration());

       var useCase = new CreateUserUseCase(
            repository,
            currentUser,
            facade,
            new FakeTransactionRunner());

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest(
                "Juan",
                "Pérez",
                "juan@example.com",
                "Vendedor"));

        Assert.Null(result);
        Assert.Null(repository.CreatedUser);
    }

    [Fact]
    public async Task ExecuteAsync_CorreoDuplicado_LanzaError()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = true,
            EmailExists = true
        };

        repository.ActiveRoles["Vendedor"] = 2;

        var useCase = CreateUseCase(repository);

        var exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () => useCase.ExecuteAsync(
                    new CreateUserRequest(
                        "Juan",
                        "Pérez",
                        "juan@example.com",
                        "Vendedor")));

        Assert.Contains(
            "Ya existe un usuario",
            exception.Message);

        Assert.Null(repository.CreatedUser);
    }

    [Fact]
    public async Task ExecuteAsync_UsuarioExistente_AgregaSufijo()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = true
        };

        repository.ActiveRoles["Vendedor"] = 2;
        repository.ExistingUsernames.Add("jperez");

        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest(
                "Juan",
                "Pérez",
                "juan@example.com",
                "Vendedor"));

        Assert.NotNull(result);
        Assert.Equal("jperez2", result.Username);
    }

    [Fact]
    public async Task ExecuteAsync_RolInvalido_LanzaError()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = true
        };

        var useCase = CreateUseCase(repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync(
                new CreateUserRequest(
                    "Juan",
                    "Pérez",
                    "juan@example.com",
                    "RolInventado")));

        Assert.Null(repository.CreatedUser);
    }

    private static CreateUserUseCase CreateUseCase(
        FakeUserRepository repository)
    {
        var currentUser = new FakeCurrentUser
        {
            UserId = 1,
            Username = "admin"
        };

        var facade = new UsuarioFacade(
            new UsernameGenerator(repository),
            new FakeEmailSender(),
            new FakeApplicationConfiguration());

        return new CreateUserUseCase(
            repository,
            currentUser,
            facade,
            new FakeTransactionRunner());
    }
}