using Libreria.Servicio.Usuarios.Domain;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class CreateUserUseCaseTests
{
    private static readonly Guid AdminPublicId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task ExecuteAsync_Admin_CreaUsuarioCompletoConKeycloakYCorreo()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.ActiveRoles["Vendedor"] = 2;
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var emailSender = new FakeEmailSender();
        var useCase = CreateUseCase(repository, identityProvider, emailSender);

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest("José", "Pérez", "jose@example.com", "Vendedor", "García"));

        Assert.NotNull(result);
        Assert.True(result.EsExito);
        Assert.Equal("jperezg", result.Valor.Username);
        Assert.NotEqual(Guid.Empty, result.Valor.PublicId);
        Assert.NotNull(repository.CreatedUser);
        Assert.Equal(UserStatuses.Pending, repository.CreatedUser.Status);
        Assert.Equal("jose@example.com", repository.CreatedUser.Email);
        Assert.Equal("García", repository.CreatedUser.MaternalLastName);
        Assert.Equal("José Pérez García", repository.CreatedUser.FullName);
        Assert.Equal(AdminPublicId, repository.CreatedUser.CreatedByUserId);
        Assert.NotNull(repository.CreatedUser.KeycloakId);
        Assert.Equal("jose@example.com", emailSender.Recipient);
        Assert.Contains(identityProvider.Usuarios.Keys, id => id == repository.CreatedUser.KeycloakId);
    }

    [Fact]
    public async Task ExecuteAsync_NombreCompuestoConEspacios_AceptaYNormaliza()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.ActiveRoles["Vendedor"] = 2;
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest(" juan  josé ", " pérez ", "juanjose@example.com", "Vendedor", " garcía "));

        Assert.NotNull(result);
        Assert.True(result.EsExito);
        Assert.Equal("jperezg", result.Valor.Username);
        Assert.Equal("Juan José", repository.CreatedUser!.FirstName);
        Assert.Equal("Pérez", repository.CreatedUser.LastName);
        Assert.Equal("García", repository.CreatedUser.MaternalLastName);
        Assert.Equal("Juan José Pérez García", repository.CreatedUser.FullName);
    }

    [Fact]
    public async Task ExecuteAsync_FalloEnvioCorreo_CompensaKeycloakYDevuelveError()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        repository.ActiveRoles["Vendedor"] = 2;
        var identityProvider = new ProveedorIdentidadEnMemoria();
        var emailSender = new FakeEmailSender { ThrowException = new InvalidOperationException("Servidor SMTP no disponible") };
        var useCase = CreateUseCase(repository, identityProvider, emailSender);

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest("Ana", "Torres", "ana@example.com", "Vendedor"));

        Assert.NotNull(result);
        Assert.False(result.EsExito);
        Assert.Contains("No fue posible enviar el correo con las credenciales temporales. Por favor, repita el proceso.", result.Errors());
        Assert.DoesNotContain(identityProvider.Usuarios.Values, u => u.Datos.Correo == "ana@example.com");
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
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest("Juan", "Pérez", "juan@example.com", "Vendedor"));

        Assert.NotNull(result);
        Assert.True(result.EsExito);
        Assert.Equal("jperez2", result.Valor.Username);
    }

    [Fact]
    public async Task ExecuteAsync_RolInvalido_DevuelveValidacion()
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(
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
    [InlineData("Juan@Pedro", "El nombre solo permite letras, incluyendo acentos y ñ.")]
    public async Task ExecuteAsync_NombreInvalido_DevuelveValidacion(
        string nombre, string mensaje)
    {
        var repository = new FakeUserRepository { IsActiveAdministrator = true };
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(
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
        var useCase = CreateUseCase(repository);

        var result = await useCase.ExecuteAsync(
            new CreateUserRequest("Juan", "Pérez", "correo inválido", "Vendedor"));

        Assert.NotNull(result);
        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Validacion, result.Tipo);
        Assert.Equal("El correo no tiene un formato válido.", result.Errores[nameof(CreateUserRequest.Email)]);
        Assert.Null(repository.CreatedUser);
    }

    private static CreateUserUseCase CreateUseCase(
        FakeUserRepository repository,
        ProveedorIdentidadEnMemoria? identityProvider = null,
        FakeEmailSender? emailSender = null)
    {
        var email = emailSender ?? new FakeEmailSender();
        var idp = identityProvider ?? new ProveedorIdentidadEnMemoria();
        var facade = new UsuarioFacade(
            new UsernameGenerator(repository),
            email,
            new FakeApplicationConfiguration(),
            idp);

        return new CreateUserUseCase(
            repository,
            new FakeCurrentUser { UserId = AdminPublicId, Username = "admin" },
            facade,
            new FakeTransactionRunner());
    }
}
