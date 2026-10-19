namespace Libreria.Application.Ports.Secondary;

public interface IEmailSender
{
    Task EnviarAsync(
        string destinatario,
        string asunto,
        string cuerpoHtml);
}