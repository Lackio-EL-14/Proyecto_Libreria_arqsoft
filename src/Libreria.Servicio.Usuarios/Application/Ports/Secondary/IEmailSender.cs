namespace Libreria.Servicio.Usuarios.Application.Ports.Secondary;

public interface IEmailSender
{
    Task SendAsync(
        string recipient,
        string subject,
        string htmlBody);
}