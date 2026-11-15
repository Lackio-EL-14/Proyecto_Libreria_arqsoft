using System.Net.Http.Json;
using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Infrastructure.Http;

namespace Libreria.Servicio.Ventas.Infrastructure.Adapters;

/// <summary>Adapter HTTP del puerto de Productos; concentra su contrato REST.</summary>
public sealed class ProductosHttpAdapter : IProductosPort
{
    private readonly HttpClient _httpClient;

    public ProductosHttpAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Result<ProductoVentaSnapshot>> ObtenerPorPublicIdAsync(
        Guid publicId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var respuesta = await _httpClient.GetAsync(
                $"api/productos/{publicId}",
                cancellationToken);
            var estado = await RespuestaServicioTraductor.AResultadoAsync(
                respuesta,
                "Productos",
                cancellationToken);

            if (!estado.EsExito)
            {
                return Result<ProductoVentaSnapshot>.Fallo(estado);
            }

            var producto = await respuesta.Content.ReadFromJsonAsync<ProductoHttpDto>(
                cancellationToken: cancellationToken);

            return producto is null
                ? Result<ProductoVentaSnapshot>.Fallo(
                    Result.NoDisponible("Productos", "Productos devolvió una respuesta vacía."))
                : Result<ProductoVentaSnapshot>.Exito(new ProductoVentaSnapshot(
                    producto.PublicId,
                    producto.Nombre,
                    producto.Stock,
                    producto.PrecioVenta,
                    producto.CostoAdquisicionActual));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<ProductoVentaSnapshot>.Fallo(
                RespuestaServicioTraductor.NoDisponible("Productos"));
        }
        catch (HttpRequestException)
        {
            return Result<ProductoVentaSnapshot>.Fallo(
                RespuestaServicioTraductor.NoDisponible("Productos"));
        }
    }

    public Task<Result> DescontarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default) =>
        CambiarStockAsync(productoPublicId, cantidad, ventaPublicId, "descontar", cancellationToken);

    public Task<Result> RestaurarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default) =>
        CambiarStockAsync(productoPublicId, cantidad, ventaPublicId, "restaurar", cancellationToken);

    private async Task<Result> CambiarStockAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId,
        string operacion,
        CancellationToken cancellationToken)
    {
        try
        {
            using var respuesta = await _httpClient.PostAsJsonAsync(
                $"api/productos/{productoPublicId}/stock/{operacion}",
                new StockRequest(cantidad, ventaPublicId),
                cancellationToken);

            return await RespuestaServicioTraductor.AResultadoAsync(
                respuesta,
                "Productos",
                cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return RespuestaServicioTraductor.NoDisponible("Productos");
        }
        catch (HttpRequestException)
        {
            return RespuestaServicioTraductor.NoDisponible("Productos");
        }
    }

    private sealed record StockRequest(int Cantidad, Guid VentaPublicId);

    private sealed record ProductoHttpDto(
        Guid PublicId,
        string Nombre,
        int Stock,
        decimal PrecioVenta,
        decimal CostoAdquisicionActual);
}
