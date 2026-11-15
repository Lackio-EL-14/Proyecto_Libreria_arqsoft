using System.Net.Http.Json;
using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Infrastructure.Http;

namespace Libreria.Servicio.Ventas.Infrastructure.Adapters;

/// <summary>Adapter HTTP del puerto de Clientes; Ventas no conoce su persistencia.</summary>
public sealed class ClientesHttpAdapter : IClientesPort
{
    private readonly HttpClient _httpClient;

    public ClientesHttpAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<ClienteVentaSnapshot>> ObtenerPorCiNitAsync(
        string ciNit,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ruta = "api/clientes/buscar?ciNit=" + Uri.EscapeDataString(ciNit.Trim());
            using var respuesta = await _httpClient.GetAsync(ruta, cancellationToken);
            var estado = await RespuestaServicioTraductor.AResultadoAsync(
                respuesta,
                "Clientes",
                cancellationToken);

            if (!estado.EsExito)
            {
                return Result<ClienteVentaSnapshot>.Fallo(estado);
            }

            var clientes = await respuesta.Content.ReadFromJsonAsync<List<ClienteVentaSnapshot>>(
                cancellationToken: cancellationToken) ?? [];
            var cliente = clientes.FirstOrDefault(item =>
                item.CiNit.Equals(ciNit.Trim(), StringComparison.OrdinalIgnoreCase));

            return cliente is null
                ? Result<ClienteVentaSnapshot>.Fallo(
                    Result.NoEncontrado("El cliente seleccionado no existe o está inactivo."))
                : Result<ClienteVentaSnapshot>.Exito(cliente);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<ClienteVentaSnapshot>.Fallo(
                RespuestaServicioTraductor.NoDisponible("Clientes"));
        }
        catch (HttpRequestException)
        {
            return Result<ClienteVentaSnapshot>.Fallo(
                RespuestaServicioTraductor.NoDisponible("Clientes"));
        }
    }
}
