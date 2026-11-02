using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class ListUsersUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_AdministradorActivo_DevuelveListadoSeguro()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = true
        };

        repository.UserList.Add(
            new UserListItemDto(
                Guid.NewGuid(),
                "Administrador del sistema",
                "admin",
                "admin@libreria.test",
                "Administrador",
                "Activo"));

        var currentUser = new FakeCurrentUser
        {
            UserId = 1,
            Username = "admin"
        };

        var useCase =
            new ListUsersUseCase(repository, currentUser);

        var result =
            await useCase.ExecuteAsync();

        Assert.NotNull(result);
        Assert.Single(result);

        var user = result[0];

        Assert.Equal("admin", user.Username);
        Assert.Equal("Administrador", user.Role);
        Assert.Equal("Activo", user.Status);
    }

    [Fact]
    public async Task ExecuteAsync_UsuarioNoAdministrador_DevuelveNull()
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

        var useCase =
            new ListUsersUseCase(repository, currentUser);

        var result =
            await useCase.ExecuteAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_EstadoValido_NormalizaYEnviaFiltro()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = true
        };

        var currentUser = new FakeCurrentUser
        {
            UserId = 1
        };

        var useCase =
            new ListUsersUseCase(repository, currentUser);

        await useCase.ExecuteAsync("  Activo  ");

        Assert.Equal(
            "Activo",
            repository.LastListStatus);
    }

    [Fact]
    public async Task ExecuteAsync_EstadoInvalido_LanzaArgumentException()
    {
        var repository = new FakeUserRepository
        {
            IsActiveAdministrator = true
        };

        var currentUser = new FakeCurrentUser
        {
            UserId = 1
        };

        var useCase =
            new ListUsersUseCase(repository, currentUser);

        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync("Suspendido"));
    }
}