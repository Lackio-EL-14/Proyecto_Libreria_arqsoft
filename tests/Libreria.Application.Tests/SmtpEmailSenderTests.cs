using Libreria.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Libreria.Application.Tests;

public class SmtpEmailSenderTests
{
    [Fact]
    public async Task Development_EntregaLocal_GeneraCorreoSinCredencialesSmtp()
    {
        var nombre = "LibreriaMailTests_" + Guid.NewGuid().ToString("N");
        var raiz = Path.GetFullPath(Path.Combine(Path.GetTempPath(), nombre));
        try
        {
            var config = Config("mail");
            var sender = new SmtpEmailSender(config, new CorreoTestEnvironment
                { EnvironmentName = Environments.Development, ContentRootPath = raiz });

            await sender.EnviarAsync("prueba@example.test", "Asunto de prueba", "<p>Contenido de prueba</p>");

            var archivo = Assert.Single(Directory.GetFiles(Path.Combine(raiz, "mail"), "*.eml"));
            var contenido = await File.ReadAllTextAsync(archivo);
            Assert.Contains("prueba@example.test", contenido);
            Assert.Contains("Asunto de prueba", contenido);
        }
        finally
        {
            if (Directory.Exists(raiz) && Path.GetDirectoryName(raiz) == Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                && Path.GetFileName(raiz) == nombre)
                Directory.Delete(raiz, recursive: true);
        }
    }

    [Fact]
    public async Task Production_EntregaLocal_RechazaSinCrearArchivos()
    {
        var sender = new SmtpEmailSender(Config("mail"), new CorreoTestEnvironment());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.EnviarAsync("prueba@example.test", "Prueba", "Contenido"));

        Assert.Equal("La entrega local de correo solo está permitida en Development.", error.Message);
    }

    [Fact]
    public async Task Development_CarpetaPublica_RechazaParaNoExponerEnlacesDeActivacion()
    {
        var sender = new SmtpEmailSender(Config("wwwroot/mail"), new CorreoTestEnvironment
            { EnvironmentName = Environments.Development });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.EnviarAsync("prueba@example.test", "Prueba", "Contenido"));

        Assert.Equal("La carpeta de correo local no puede estar dentro de wwwroot.", error.Message);
    }

    [Fact]
    public async Task Production_SinModoLocal_ContinuaExigiendoSmtp()
    {
        var sender = new SmtpEmailSender(new ConfigurationBuilder().Build(), new CorreoTestEnvironment());

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sender.EnviarAsync("prueba@example.test", "Prueba", "Contenido"));

        Assert.Equal("Falta la configuración SMTP_HOST.", error.Message);
    }

    private static IConfiguration Config(string carpeta) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EMAIL_DELIVERY_MODE"] = "PickupDirectory",
            ["EMAIL_PICKUP_DIRECTORY"] = carpeta,
            ["SMTP_FROM"] = "noreply@libreria.local"
        }).Build();
}

internal sealed class CorreoTestEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;
    public string ApplicationName { get; set; } = "Libreria.Tests";
    public string ContentRootPath { get; set; } = Path.GetFullPath(Directory.GetCurrentDirectory());
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
