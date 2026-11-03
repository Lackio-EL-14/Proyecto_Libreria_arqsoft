using System.Net;

namespace Libreria.Client.Clients.Common;


public class ApiClientException : Exception
{
    public ApiClientException(
        string servicio,
        HttpStatusCode statusCode,
        string message,
        IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Servicio = servicio;
        StatusCode = statusCode;
        Errors = errors;
    }

    public string Servicio { get; }
    public HttpStatusCode StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }
}


public sealed class ServicioNoDisponibleException : Exception
{
    public ServicioNoDisponibleException(string servicio, Exception innerException)
        : base($"El servicio de {servicio} no está disponible en este momento.", innerException)
    {
        Servicio = servicio;
    }

    public string Servicio { get; }
}
