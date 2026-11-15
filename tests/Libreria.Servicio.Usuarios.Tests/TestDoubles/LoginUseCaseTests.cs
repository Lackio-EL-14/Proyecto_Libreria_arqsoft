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

        Assert.NotNull(result);
        Assert.Equal(7, result.UserId);
        Assert.Equal("jperez", result.Username);
        Assert.Equal("Juan Pérez", result.FullName);
        Assert.Equal("Administrador", result.Role);
    }

    [Fact]
    public async Task ExecuteAsync_ContrasenaIncorrecta_DevuelveNull()
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

        Assert.Null(result);
    }
}