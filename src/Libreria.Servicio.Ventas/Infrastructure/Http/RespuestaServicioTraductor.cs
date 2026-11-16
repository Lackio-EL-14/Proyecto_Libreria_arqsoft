using System.Net;
using System.Net.Http.Json;
using Libreria.Servicio.Ventas.Application.Comun;

namespace Libreria.Servicio.Ventas.Infrastructure.Http;

internal static class RespuestaServicioTraductor
{
    public static async Task<Result> AResultadoAsync(
        HttpResponseMessage respuesta,
        string servicio,
        CancellationToken cancellationToken)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return Result.Exito();
        }

        var detalle = await LeerDetalleAsync(respuesta, cancellationToken);

        return respuesta.StatusCode switch
        {
            HttpStatusCode.NotFound => Result.NoEncontrado(detalle ?? "El recurso solicitado no existe."),
            HttpStatusCode.Conflict => Result.Conflicto(servicio, detalle ?? "La operación entra en conflicto con el estado actual."),
            HttpStatusCode.BadRequest => Result.Validacion(servicio, detalle ?? "El servicio rechazó los datos enviados."),
            _ => Result.NoDisponible(servicio, detalle ?? $"El servicio de {servicio} no está disponible.")
        };
    }

    public static Result NoDisponible(string servicio) =>
        Result.NoDisponible(servicio, $"No se pudo conectar con el servicio de {servicio}.");

    private static async Task<string?> LeerDetalleAsync(
        HttpResponseMessage respuesta,
        CancellationToken cancellationToken)
    {
        try
        {
            var problema = await respuesta.Content.ReadFromJsonAsync<ProblemaHttp>(cancellationToken);
            return problema?.Detail ?? problema?.Title;
        }
        catch (Exception error) when (error is System.Text.Json.JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private sealed record ProblemaHttp(string? Title, string? Detail);
}
