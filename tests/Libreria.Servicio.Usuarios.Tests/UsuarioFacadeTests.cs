using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class UsuarioFacadeTests
{
    [Fact]
    public async Task PrepareActivationAsync_GeneraUsuarioTokenYHash()
    {
        var repository = new FakeUserRepository();

        var generator =
            new UsernameGenerator(repository);

        var facade = new UsuarioFacade(
            generator,
            new FakeEmailSender(),
            new FakeApplicationConfiguration());

        var result =
            await facade.PrepareActivationAsync(
                "Juan",
                "Pérez");

        Assert.Equal("jperez", result.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.False(string.IsNullOrWhiteSpace(result.TokenHash));

        Assert.Equal(
            ActivationTokenService.CalculateHash(result.Token),
            result.TokenHash);

        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task SendActivationEmailAsync_UsaCorreoYUrlDelFront()
    {
        var repository = new FakeUserRepository();

        var emailSender = new FakeEmailSender();

        var configuration =
            new FakeApplicationConfiguration
            {
                BaseUrl = "https://localhost:5000"
            };

        var facade = new UsuarioFacade(
            new UsernameGenerator(repository),
            emailSender,
            configuration);

        var activation =
            new UserActivationPreparation(
                "jperez",
                "token123",
                "HASH123",
                DateTime.UtcNow.AddHours(24));

        await facade.SendActivationEmailAsync(
            "juan@example.com",
            "Juan",
            activation);

        Assert.Equal(
            "juan@example.com",
            emailSender.Recipient);

        Assert.Contains(
            "Sistema Librería",
            emailSender.Subject);

        Assert.Contains(
            "jperez",
            emailSender.HtmlBody);

        Assert.Contains(
            "https://localhost:5000/Usuarios/Activar",
            emailSender.HtmlBody);
    }
}