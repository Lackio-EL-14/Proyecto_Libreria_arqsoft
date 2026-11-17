using Libreria.Servicio.Usuarios.Application.Comun;
using Libreria.Servicio.Usuarios.Application.DTOs.Auth;
using Libreria.Servicio.Usuarios.Application.UseCases.Auth;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class LoginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CredencialesValidas_DevuelveUsuario()
    {
        var repository = new FakeUserRepository
        {
            UserByUsername = new User
            {
                Id = 7,
                PublicId = Guid.NewGuid(),
                Username = "jperez",
                FullName = "Juan Pérez",
                PasswordHash = "hash",
                Status = UserStatuses.Active,
                Role = new Role
                {
                    Id = 1,
                    Name = "Administrador",
                    IsActive = true
                }
            }
        };

        var hasher = new FakePasswordHasher
        {
            VerifyResult = true,
            RequiresRehashResult = false
        };

        var useCase = new LoginUseCase(
            repository,
            hasher,
            new LoginValidator());

        var result = await useCase.ExecuteAsync(
            new LoginRequest
            {
                Username = "jperez",
                Password = "Password123!"
            });

        Assert.True(result.EsExito);
        Assert.Equal(7, result.Valor.UserId);
        Assert.Equal("jperez", result.Valor.Username);
        Assert.Equal("Juan Pérez", result.Valor.FullName);
        Assert.Equal("Administrador", result.Valor.Role);
    }

    [Fact]
    public async Task ExecuteAsync_ContrasenaIncorrecta_DevuelveResultValidacion()
    {
        var repository = new FakeUserRepository
        {
            UserByUsername = new User
            {
                Id = 7,
                Username = "jperez",
                PasswordHash = "hash",
                Status = UserStatuses.Active,
                Role = new Role
                {
                    Name = "Administrador",
                    IsActive = true
                }
            }
        };

        var hasher = new FakePasswordHasher
        {
            VerifyResult = false
        };

        var useCase = new LoginUseCase(
            repository,
            hasher,
            new LoginValidator());

        var result = await useCase.ExecuteAsync(
            new LoginRequest
            {
                Username = "jperez",
                Password = "incorrecta"
            });

        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Validacion, result.Tipo);
        Assert.Equal("Usuario o contraseña incorrectos.", result.Errores["Credenciales"]);
    }

    [Fact]
    public async Task ExecuteAsync_CamposVacios_DevuelveErroresSinLanzarExcepcion()
    {
        var useCase = new LoginUseCase(
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new LoginValidator());

        var result = await useCase.ExecuteAsync(new LoginRequest
        {
            Username = "   ",
            Password = ""
        });

        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Validacion, result.Tipo);
        Assert.Equal("El nombre de usuario es obligatorio.", result.Errores["Username"]);
        Assert.Equal("La contraseña es obligatoria.", result.Errores["Password"]);
    }

    [Fact]
    public async Task ExecuteAsync_CamposMuyLargos_DevuelveErroresSinLanzarExcepcion()
    {
        var useCase = new LoginUseCase(
            new FakeUserRepository(),
            new FakePasswordHasher(),
            new LoginValidator());

        var result = await useCase.ExecuteAsync(new LoginRequest
        {
            Username = new string('a', 51),
            Password = new string('b', 129)
        });

        Assert.False(result.EsExito);
        Assert.Equal(TipoError.Validacion, result.Tipo);
        Assert.Equal("El nombre de usuario no puede superar los 50 caracteres.", result.Errores["Username"]);
        Assert.Equal("La contraseña no puede superar los 128 caracteres.", result.Errores["Password"]);
    }
}