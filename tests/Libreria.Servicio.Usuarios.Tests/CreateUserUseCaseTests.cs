using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Comun;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class CreateUserUseCaseTests
{
    private static readonly Guid AdminPublicId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task ExecuteAsync_Admin_CreaUsuarioPendienteSinTokenNiCorreo()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.ActiveRoles["Vendedor"] = 2;
        var currentUser = new FakeCurrentUser { UserId = AdminPublicId, Username = "admin" };
        var emailSender = new FakeEmailSender();
        var useCase = new CreateUserUseCase(
            repository, currentUser, new UsuarioFacade(new UsernameGenerator(repository), emailSender, new FakeApplicationConfiguration()),
            new FakeTransactionRunner());

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest("José", "Pérez", "jose@example.com", "Vendedor", "García"));

        Assert.NotNull(result);
        Assert.True(result.EsExito);
        Assert.Equal("jperez", result.Valor.Username);
        Assert.NotEqual(Guid.Empty, result.Valor.PublicId);
        Assert.NotNull(repository.CreatedUser);
        Assert.Equal(UserStatuses.Pending, repository.CreatedUser.Status);
        Assert.Equal("jose@example.com", repository.CreatedUser.Email);
        Assert.Equal("García", repository.CreatedUser.MaternalLastName);
        Assert.Equal("José Pérez García", repository.CreatedUser.FullName);
        Assert.Equal(AdminPublicId, repository.CreatedUser.CreatedByUserId);
        Assert.Null(repository.CreatedUser.KeycloakId);
        Assert.Null(emailSender.Recipient); // US-124 no envía correo antes de US-130.
    }

    [Fact]
    public async Task ExecuteAsync_NoAdministrador_NoCreaUsuario()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = false };
        var useCase = CreateUseCase(repository);
        var result = await useCase.ExecuteAsync(
            new CreateUserRequest("Juan", "Pérez", "juan@example.com", "Vendedor"));
        Assert.Null(result);
        Assert.Null(repository.CreatedUser);
    }

    [Fact]
    public async Task ExecuteAsync_CorreoDuplicado_DevuelveConflicto()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true, EmailExists = true };
        repository.ActiveRoles["Vendedor"] = 2;
        var useCase = CreateUseCase(repository);
        var result = await useCase.ExecuteAsync(
            new CreateUserRequest("Juan", "Pérez", "juan@example.com", "Vendedor"));
        Assert.NotNull(result);
        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Conflicto, result.Tipo);
        Assert.Contains("Ya existe un usuario", result.Errores[nameof(CreateUserRequest.Email)]);
        Assert.Null(repository.CreatedUser);
    }

    [Fact]
    public async Task ExecuteAsync_UsuarioExistente_AgregaSufijo()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.ActiveRoles["Vendedor"] = 2;
        repository.ExistingUsernames.Add("jperez");
        var result = await CreateUseCase(repository).ExecuteAsync(
            new CreateUserRequest("Juan", "Pérez", "juan@example.com", "Vendedor"));
        Assert.NotNull(result);
        Assert.True(result.EsExito);
        Assert.Equal("jperez2", result.Valor.Username);
    }

    [Fact]
    public async Task ExecuteAsync_RolInvalido_DevuelveValidacion()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        var result = await CreateUseCase(repository).ExecuteAsync(
            new CreateUserRequest("Juan", "Pérez", "juan@example.com", "RolInventado"));
        Assert.NotNull(result);
        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Validacion, result.Tipo);
        Assert.Equal("Debe seleccionar un rol válido.", result.Errores[nameof(CreateUserRequest.Role)]);
        Assert.Null(repository.CreatedUser);
    }

    [Theory]
    [InlineData("", "El nombre es obligatorio.")]
    [InlineData("Juan1", "El nombre solo permite letras, incluyendo acentos y ñ.")]
    public async Task ExecuteAsync_NombreInvalido_DevuelveValidacion(
        string nombre, string mensaje)
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        var result = await CreateUseCase(repository).ExecuteAsync(
            new CreateUserRequest(nombre, "Pérez", "juan@example.com", "Vendedor"));

        Assert.NotNull(result);
        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Validacion, result.Tipo);
        Assert.Equal(mensaje, result.Errores[nameof(CreateUserRequest.FirstName)]);
        Assert.Null(repository.CreatedUser);
    }

    [Fact]
    public async Task ExecuteAsync_CorreoInvalido_DevuelveValidacion()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        var result = await CreateUseCase(repository).ExecuteAsync(
            new CreateUserRequest("Juan", "Pérez", "correo inválido", "Vendedor"));

        Assert.NotNull(result);
        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Validacion, result.Tipo);
        Assert.Equal("El correo no tiene un formato válido.",
            result.Errores[nameof(CreateUserRequest.Email)]);
        Assert.Null(repository.CreatedUser);
    }

    private static CreateUserUseCase CreateUseCase(FakeUserRepository repository) =>
        new(repository,
            new FakeCurrentUser { UserId = AdminPublicId, Username = "admin" },
            new UsuarioFacade(new UsernameGenerator(repository), new FakeEmailSender(), new FakeApplicationConfiguration()),
            new FakeTransactionRunner());
}
