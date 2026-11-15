using System.Net;
using System.Net.Mail;
using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public SmtpEmailSender(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task SendAsync(
        string recipient,
        string subject,
        string htmlBody)
    {
        if (string.Equals(
                _configuration["EMAIL_DELIVERY_MODE"],
                "PickupDirectory",
                StringComparison.OrdinalIgnoreCase))
        {
            await SendToPickupDirectoryAsync(
                recipient,
                subject,
                htmlBody);

            return;
        }

        var host = GetRequired("SMTP_HOST");
        var username = GetRequired("SMTP_USER");
        var password = GetRequired("SMTP_PASS");
        var from = GetRequired("SMTP_FROM");

        if (!int.TryParse(
                GetRequired("SMTP_PORT"),
                out var port))
        {
            throw new InvalidOperationException(
                "SMTP_PORT no contiene un puerto válido.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(from),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(recipient);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = true,
            Credentials =
                new NetworkCredential(username, password)
        };

        await client.SendMailAsync(message);
    }

    private async Task SendToPickupDirectoryAsync(
        string recipient,
        string subject,
        string htmlBody)
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "La entrega local de correo solo está permitida en Development.");
        }

        var configuredDirectory =
            GetRequired("EMAIL_PICKUP_DIRECTORY");

        var directory = Path.GetFullPath(
            configuredDirectory,
            _environment.ContentRootPath);

        var publicDirectory = Path.Combine(
            _environment.ContentRootPath,
            "wwwroot");

        if (directory.Equals(
                publicDirectory,
                StringComparison.OrdinalIgnoreCase) ||
            directory.StartsWith(
                publicDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "La carpeta de correo local no puede estar dentro de wwwroot.");
        }

        Directory.CreateDirectory(directory);

        using var message = new MailMessage
        {
            From = new MailAddress(
                GetRequired("SMTP_FROM")),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(recipient);

        using var client = new SmtpClient
        {
            DeliveryMethod =
                SmtpDeliveryMethod.SpecifiedPickupDirectory,
            PickupDirectoryLocation = directory,
            EnableSsl = false
        };

        await client.SendMailAsync(message);
    }

    private string GetRequired(string key)
    {
        return _configuration[key]
            ?? throw new InvalidOperationException(
                $"Falta la configuración {key}.");
    }
}