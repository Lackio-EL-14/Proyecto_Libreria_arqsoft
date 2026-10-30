using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeEmailSender : IEmailSender
{
    public string? Recipient { get; private set; }
    public string? Subject { get; private set; }
    public string? HtmlBody { get; private set; }

    public Task SendAsync(
        string recipient,
        string subject,
        string htmlBody)
    {
        Recipient = recipient;
        Subject = subject;
        HtmlBody = htmlBody;

        return Task.CompletedTask;
    }
}

public sealed class FakeApplicationConfiguration
    : IApplicationConfiguration
{
    public string BaseUrl { get; set; } =
        "https://localhost:5000";
}