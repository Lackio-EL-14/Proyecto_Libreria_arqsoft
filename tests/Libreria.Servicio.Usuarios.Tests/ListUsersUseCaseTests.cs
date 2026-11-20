using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class ListUsersUseCaseTests
{
    private static readonly Guid AdminPublicId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task ExecuteAsync_AdministradorActivo_DevuelveListadoSeguro()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.UserList.Add(
            new UserListItemDto(
                Guid.NewGuid(),
                "Administrador del sistema",
                "admin",
                "admin@libreria.test",
                "Administrador",
                "Activo"));

        var currentUser = new FakeCurrentUser { UserId = AdminPublicId, Username = "admin" };
        var useCase = new ListUsersUseCase(repository, currentUser);

        var result = await useCase.ExecuteAsync();

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
        var repository = new FakeUserRepository { IsActiveAdministrator = false };
        var currentUser = new FakeCurrentUser { UserId = Guid.NewGuid(), Username = "vendedor" };
        var useCase = new ListUsersUseCase(repository, currentUser);

        var result = await useCase.ExecuteAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_EstadoValido_NormalizaYEnviaFiltro()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        var currentUser = new FakeCurrentUser { UserId = AdminPublicId };
        var useCase = new ListUsersUseCase(repository, currentUser);

        await useCase.ExecuteAsync("  Activo  ");

        Assert.Equal("Activo", repository.LastListStatus);
    }

    [Fact]
    public async Task ExecuteAsync_EstadoInvalido_LanzaArgumentException()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        var currentUser = new FakeCurrentUser { UserId = AdminPublicId };
        var useCase = new ListUsersUseCase(repository, currentUser);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("Suspendido"));
    }

    [Fact]
    public async Task ExecuteAsync_UsuarioConCambioPendienteFalso_PasaAEstadoActivo()
    {
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var userPublicId = Guid.NewGuid();
        var createResult = await identityProvider.CrearUsuarioAsync(
            new UsuarioIdentidad("jperez", "jperez@libreria.local", "Juan", "Pérez"));
        var keycloakId = createResult.Valor;
        await identityProvider.FijarContrasenaAsync(keycloakId, "Definitiva123!", temporal: false);

        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.UserList.Add(
            new UserListItemDto(
                keycloakId,
                "Juan Pérez",
                "jperez",
                "jperez@libreria.local",
                "Vendedor",
                "Pendiente"));

        var currentUser = new FakeCurrentUser { UserId = AdminPublicId, Username = "admin" };
        var useCase = new ListUsersUseCase(repository, currentUser, identityProvider);

        var result = await useCase.ExecuteAsync();

        Assert.NotNull(result);
        var user = Assert.Single(result);
        Assert.Equal("Activo", user.Status);
    }

    [Fact]
    public async Task ExecuteAsync_UsuarioConCambioPendienteVerdadero_PermanecePendiente()
    {
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var createResult = await identityProvider.CrearUsuarioAsync(
            new UsuarioIdentidad("mrojas", "mrojas@libreria.local", "María", "Rojas"));
        var keycloakId = createResult.Valor;
        await identityProvider.FijarContrasenaAsync(keycloakId, "Temporal#2026x", temporal: true);

        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.UserList.Add(
            new UserListItemDto(
                keycloakId,
                "María Rojas",
                "mrojas",
                "mrojas@libreria.local",
                "Vendedor",
                "Pendiente"));

        var currentUser = new FakeCurrentUser { UserId = AdminPublicId, Username = "admin" };
        var useCase = new ListUsersUseCase(repository, currentUser, identityProvider);

        var result = await useCase.ExecuteAsync();

        Assert.NotNull(result);
        var user = Assert.Single(result);
        Assert.Equal("Pendiente", user.Status);
    }

    [Fact]
    public async Task ExecuteAsync_FiltroActivo_IncluyeUsuarioQueCambioContrasena()
    {
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var createResult = await identityProvider.CrearUsuarioAsync(
            new UsuarioIdentidad("csuarez", "csuarez@libreria.local", "Carlos", "Suárez"));
        var keycloakId = createResult.Valor;
        await identityProvider.FijarContrasenaAsync(keycloakId, "Definitiva123!", temporal: false);

        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.UserList.Add(
            new UserListItemDto(
                keycloakId,
                "Carlos Suárez",
                "csuarez",
                "csuarez@libreria.local",
                "Vendedor",
                "Pendiente"));

        var currentUser = new FakeCurrentUser { UserId = AdminPublicId, Username = "admin" };
        var useCase = new ListUsersUseCase(repository, currentUser, identityProvider);

        var result = await useCase.ExecuteAsync("Activo");

        Assert.NotNull(result);
        var user = Assert.Single(result);
        Assert.Equal("Activo", user.Status);
    }
}
