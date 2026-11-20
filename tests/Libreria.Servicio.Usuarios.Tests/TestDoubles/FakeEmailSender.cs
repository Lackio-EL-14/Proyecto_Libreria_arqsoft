using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeEmailSender : IEmailSender
{
    public string? Recipient { get; private set; }
    public string? Subject { get; private set; }
    public string? HtmlBody { get; private set; }
    public Exception? ThrowException { get; set; }

    public Task SendAsync(string recipient, string subject, string htmlBody)
    {
        if (ThrowException is not null)
        {
            throw ThrowException;
        }

        Recipient = recipient;
        Subject = subject;
        HtmlBody = htmlBody;
        return Task.CompletedTask;
    }
}
