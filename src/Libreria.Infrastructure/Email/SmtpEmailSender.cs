using System.Net;
using System.Net.Mail;
using Libreria.Application.Ports.Secondary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Libreria.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public SmtpEmailSender(IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml)
    {
        if (string.Equals(_configuration["EMAIL_DELIVERY_MODE"], "PickupDirectory", StringComparison.OrdinalIgnoreCase))
        {
            if (!_environment.IsDevelopment())
                throw new InvalidOperationException("La entrega local de correo solo está permitida en Development.");

            var carpeta = Path.GetFullPath(ObtenerConfiguracion("EMAIL_PICKUP_DIRECTORY"), _environment.ContentRootPath);
            var carpetaPublica = Path.Combine(_environment.ContentRootPath, "wwwroot");
            if (carpeta.Equals(carpetaPublica, StringComparison.OrdinalIgnoreCase) ||
                carpeta.StartsWith(carpetaPublica + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La carpeta de correo local no puede estar dentro de wwwroot.");

            Directory.CreateDirectory(carpeta);
            using var mensajeLocal = new MailMessage(ObtenerConfiguracion("SMTP_FROM"), destinatario)
            {
                Subject = asunto, Body = cuerpoHtml, IsBodyHtml = true
            };
            using var clienteLocal = new SmtpClient
            {
                DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = carpeta,
                EnableSsl = false
            };
            await clienteLocal.SendMailAsync(mensajeLocal);
            return;
        }

        var host = ObtenerConfiguracion("SMTP_HOST");
        var usuario = ObtenerConfiguracion("SMTP_USER");
        var contrasena = ObtenerConfiguracion("SMTP_PASS");
        var remitente = ObtenerConfiguracion("SMTP_FROM");

        var puertoTexto = ObtenerConfiguracion("SMTP_PORT");

        if (!int.TryParse(puertoTexto, out var puerto))
            throw new InvalidOperationException("SMTP_PORT no es válido.");

        using var mensaje = new MailMessage
        {
            From = new MailAddress(remitente),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true
        };

        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(host, puerto)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(usuario, contrasena)
        };

        await cliente.SendMailAsync(mensaje);
    }

    private string ObtenerConfiguracion(string clave)
    {
        return _configuration[clave]
            ?? throw new InvalidOperationException(
                $"Falta la configuración {clave}.");
    }
}
