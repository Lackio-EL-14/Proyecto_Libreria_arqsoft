namespace Libreria.Servicio.Usuarios.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(
        string recipient,
        string subject,
        string htmlBody);
}