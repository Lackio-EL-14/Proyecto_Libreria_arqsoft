using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class UsuarioFacadeTests
{
    [Fact]
    public async Task GenerateUsernameAsync_UsaGeneradorYRespetaDuplicados()
    {
        var repository = new FakeUserRepository();
        repository.ExistingUsernames.Add("jperez");
        var facade = new UsuarioFacade(
            new UsernameGenerator(repository),
            new FakeEmailSender(),
            new FakeApplicationConfiguration());

        var username = await facade.GenerateUsernameAsync("Juan", "Pérez");

        Assert.Equal("jperez2", username);
    }

    [Fact]
    public async Task SendTemporaryCredentialsEmailAsync_UsaUrlDelFrontYEscapaHtml()
    {
        var sender = new FakeEmailSender();
        var facade = new UsuarioFacade(
            new UsernameGenerator(new FakeUserRepository()),
            sender,
            new FakeApplicationConfiguration { BaseUrl = "https://libreria.example/" });

        await facade.SendTemporaryCredentialsEmailAsync(
            "juan@example.com", "Juan <Prueba>", "jperez", "Abc<123>!");

        Assert.Equal("juan@example.com", sender.Recipient);
        Assert.Contains("Acceso temporal", sender.Subject);
        Assert.Contains("jperez", sender.HtmlBody);
        Assert.Contains("Juan &lt;Prueba&gt;", sender.HtmlBody);
        Assert.Contains("Abc&lt;123&gt;!", sender.HtmlBody);
        Assert.Contains("https://libreria.example/Cuenta/Login", sender.HtmlBody);
        Assert.DoesNotContain("/Usuarios/Activar", sender.HtmlBody);
    }

    [Fact]
    public async Task SendTemporaryCredentialsEmailAsync_NoAceptaUrlNoHttp()
    {
        var sender = new FakeEmailSender();
        var facade = new UsuarioFacade(
            new UsernameGenerator(new FakeUserRepository()),
            sender,
            new FakeApplicationConfiguration { BaseUrl = "javascript:alert(1)" });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            facade.SendTemporaryCredentialsEmailAsync(
                "juan@example.com", "Juan", "jperez", "Clave!123"));

        Assert.Null(sender.Recipient);
    }
}
