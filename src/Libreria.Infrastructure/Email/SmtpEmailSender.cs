using System.Net;
using System.Net.Mail;
using Libreria.Application.Ports.Secondary;
using Microsoft.Extensions.Configuration;

namespace Libreria.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml)
    {
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