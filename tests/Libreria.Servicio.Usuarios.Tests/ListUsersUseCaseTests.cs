using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
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
            UserId = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
            Username = "admin"
        };

        var useCase =
            new ListUsersUseCase(repository, currentUser, new ProveedorIdentidadEnMemoria());

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
            UserId = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
            Username = "vendedor"
        };

        var useCase =
            new ListUsersUseCase(repository, currentUser, new ProveedorIdentidadEnMemoria());

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
            UserId = Guid.Parse("a0000000-0000-0000-0000-000000000001")
        };

        var useCase =
            new ListUsersUseCase(repository, currentUser, new ProveedorIdentidadEnMemoria());

        await useCase.ExecuteAsync("  Activo  ");

        Assert.Null(repository.LastListStatus);
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
            UserId = Guid.Parse("a0000000-0000-0000-0000-000000000001")
        };

        var useCase =
            new ListUsersUseCase(repository, currentUser, new ProveedorIdentidadEnMemoria());

        await Assert.ThrowsAsync<ArgumentException>(
            () => useCase.ExecuteAsync("Suspendido"));
    }

    [Theory]
    [InlineData(true, "Pendiente", "Pendiente", 1)]
    [InlineData(false, "Activo", "Activo", 1)]
    [InlineData(false, "Activo", "Pendiente", 0)]
    public async Task SincronizaAntesDeFiltrar(bool temporal, string expected, string filter, int count)
    {
        var identity = new ProveedorIdentidadEnMemoria();
        var id = ProveedorIdentidadEnMemoria.IdVendedorSemilla;
        await identity.FijarContrasenaAsync(id, "Prueba123!", temporal);
        var user = new Libreria.Servicio.Usuarios.Domain.Entities.User
        {
            PublicId = Guid.NewGuid(), KeycloakId = id, Status = "Pendiente"
        };
        var repository = new FakeUserRepository { IsActiveAdministrator = true, UserByPublicId = user };
        repository.UserList.Add(new UserListItemDto(user.PublicId, "Prueba", "prueba", "test@test.local", "Vendedor", "Pendiente"));
        var useCase = new ListUsersUseCase(repository,
            new FakeCurrentUser { UserId = ProveedorIdentidadEnMemoria.IdAdminSemilla }, identity);
        var result = await useCase.ExecuteAsync(filter);
        Assert.NotNull(result);
        Assert.Equal(count, result.Count);
        if (count > 0) Assert.Equal(expected, result[0].Status);
        Assert.Equal(expected, user.Status);
        Assert.Equal(temporal ? (Guid?)null : id, repository.ActivatedKeycloakId);
    }

    [Fact]
    public async Task Inactivo_NoConsultaKeycloakNiLoReactiva()
    {
        var identity = new ProveedorIdentidadEnMemoria { FallarSiguienteLlamada = new Exception("No debe consultarse") };
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.UserList.Add(new UserListItemDto(Guid.NewGuid(), "Baja", "baja", "baja@test.local", "Vendedor", "Inactivo"));
        var useCase = new ListUsersUseCase(repository,
            new FakeCurrentUser { UserId = ProveedorIdentidadEnMemoria.IdAdminSemilla }, identity);
        var result = await useCase.ExecuteAsync("Inactivo");
        Assert.Single(result!);
        Assert.Null(repository.ActivatedKeycloakId);
    }
}