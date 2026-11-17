using System.Net;
using System.Text.Json;
using Libreria.Client.DTOs.Common;

namespace Libreria.Client.Clients.Common;

/// <summary>
/// Traduce la respuesta HTTP de una escritura a <see cref="ResultadoApi"/>:
/// 2xx → correcto, 404 → no existe, 409 → mensaje, 400 → errores por campo.
/// </summary>
internal static class ResultadoApiTraductor
{
    public static async Task<ResultadoApi> ConvertirAsync(
        HttpResponseMessage respuesta, string servicio, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return ResultadoApi.Correcto();
        }

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            var mensajeNotFound = await ExtraerMensajeAsync(respuesta, ct);
            return !string.IsNullOrWhiteSpace(mensajeNotFound)
                ? ResultadoApi.Invalido(mensajeNotFound)
                : ResultadoApi.NoExiste();
        }

        if (respuesta.StatusCode == HttpStatusCode.Conflict)
        {
            var mensajeConflict = await ExtraerMensajeAsync(respuesta, ct);
            return ResultadoApi.Invalido(
                !string.IsNullOrWhiteSpace(mensajeConflict) ? mensajeConflict : "La operación entra en conflicto con el estado actual.");
        }

        if (respuesta.StatusCode == HttpStatusCode.BadRequest)
        {
            var errores = await LeerErroresAsync(respuesta, ct);
            if (errores.Count > 0)
            {
                return ResultadoApi.Invalido(errores);
            }

            var mensajeBadRequest = await ExtraerMensajeAsync(respuesta, ct);
            if (!string.IsNullOrWhiteSpace(mensajeBadRequest))
            {
                return ResultadoApi.Invalido(mensajeBadRequest);
            }
        }

        return ResultadoApi.Invalido(
            $"El servicio de {servicio} no pudo completar la operación (HTTP {(int)respuesta.StatusCode}).");
    }

    private static async Task<string?> ExtraerMensajeAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(
                await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            if (doc.RootElement.TryGetProperty("mensaje", out var propMensaje))
            {
                return propMensaje.GetString();
            }
            if (doc.RootElement.TryGetProperty("detail", out var propDetail))
            {
                return propDetail.GetString();
            }
        }
        catch (JsonException)
        {
        }
        catch (InvalidOperationException)
        {
        }

        return null;
    }

    // Acepta los dos formatos de 400: el diccionario plano { "Input.Nombre": "mensaje" }
    // que usan los servicios y el ValidationProblemDetails ({ "errors": { "Nombre": ["mensaje"] } }).
    private static async Task<IReadOnlyDictionary<string, string>> LeerErroresAsync(
        HttpResponseMessage respuesta, CancellationToken ct)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(
                await respuesta.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<string, string>();
            }

            return doc.RootElement.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object
                ? LeerErroresDeProblemDetails(errores)
                : LeerErroresPlanos(doc.RootElement);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private static Dictionary<string, string> LeerErroresPlanos(JsonElement raiz) =>
        raiz.EnumerateObject()
            .All(campo => campo.Value.ValueKind == JsonValueKind.String)
            ? raiz.EnumerateObject().ToDictionary(campo => campo.Name, campo => campo.Value.GetString()!)
            : new Dictionary<string, string>();

    private static Dictionary<string, string> LeerErroresDeProblemDetails(JsonElement errores) =>
        errores.EnumerateObject()
            .Select(campo => (Clave: ClaveDelFormulario(campo.Name), Mensaje: PrimerMensaje(campo.Value)))
            .Where(error => error.Mensaje is not null)
            .GroupBy(error => error.Clave)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.First().Mensaje!);

    // Las vistas registran los errores con el prefijo "Input." (por ejemplo, "Input.Nombre").
    private static string ClaveDelFormulario(string campo)
    {
        var nombre = campo.TrimStart('$', '.');
        if (nombre.Length == 0)
        {
            return string.Empty;
        }

        if (nombre.StartsWith("Input.", StringComparison.OrdinalIgnoreCase))
        {
            return nombre;
        }

        return "Input." + char.ToUpperInvariant(nombre[0]) + nombre[1..];
    }

    private static string? PrimerMensaje(JsonElement valor) => valor.ValueKind switch
    {
        JsonValueKind.String => valor.GetString(),
        JsonValueKind.Array => valor.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .FirstOrDefault(),
        _ => null
    };
}
