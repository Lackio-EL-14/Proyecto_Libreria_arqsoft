using Libreria.Servicio.Ventas.Application.Comun;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Application.Facades;

public sealed class StockFacade : IStockFacade
{
    private readonly IProductosPort _productos;

    public StockFacade(IProductosPort productos)
    {
        _productos = productos;
    }

    public Task<Result> DescontarAsync(
        IReadOnlyList<StockOperacionItem> lineas,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default) =>
        EjecutarConCompensacionAsync(
            lineas,
            ventaPublicId,
            _productos.DescontarStockAsync,
            _productos.RestaurarStockAsync,
            "descontar",
            cancellationToken);

    public Task<Result> RestaurarAsync(
        IReadOnlyList<StockOperacionItem> lineas,
        Guid ventaPublicId,
        CancellationToken cancellationToken = default) =>
        EjecutarConCompensacionAsync(
            lineas,
            ventaPublicId,
            _productos.RestaurarStockAsync,
            _productos.DescontarStockAsync,
            "restaurar",
            cancellationToken);

    private static async Task<Result> EjecutarConCompensacionAsync(
        IReadOnlyList<StockOperacionItem> lineas,
        Guid ventaPublicId,
        Func<Guid, int, Guid, CancellationToken, Task<Result>> operacion,
        Func<Guid, int, Guid, CancellationToken, Task<Result>> compensacion,
        string nombreOperacion,
        CancellationToken cancellationToken)
    {
        var validacion = Validar(lineas, ventaPublicId);
        if (!validacion.EsExito)
        {
            return validacion;
        }

        var completadas = new List<StockOperacionItem>(lineas.Count);

        foreach (var linea in lineas)
        {
            var resultado = await operacion(
                linea.ProductoPublicId,
                linea.Cantidad,
                ventaPublicId,
                cancellationToken);

            if (resultado.EsExito)
            {
                completadas.Add(linea);
                continue;
            }

            var resultadoCompensacion = await CompensarAsync(
                completadas,
                ventaPublicId,
                compensacion,
                cancellationToken);

            return resultadoCompensacion.EsExito
                ? resultado
                : Result.NoDisponible(
                    "Productos",
                    $"Falló la operación de {nombreOperacion} stock y no fue posible compensarla por completo.");
        }

        return Result.Exito();
    }

    private static async Task<Result> CompensarAsync(
        IReadOnlyList<StockOperacionItem> completadas,
        Guid ventaPublicId,
        Func<Guid, int, Guid, CancellationToken, Task<Result>> compensacion,
        CancellationToken cancellationToken)
    {
        foreach (var linea in completadas.Reverse())
        {
            var resultado = await compensacion(
                linea.ProductoPublicId,
                linea.Cantidad,
                ventaPublicId,
                cancellationToken);

            if (!resultado.EsExito)
            {
                return resultado;
            }
        }

        return Result.Exito();
    }

    private static Result Validar(IReadOnlyList<StockOperacionItem>? lineas, Guid ventaPublicId)
    {
        if (ventaPublicId == Guid.Empty)
        {
            return Result.Validacion("VentaPublicId", "El identificador de la venta es obligatorio.");
        }

        if (lineas is null || lineas.Count == 0)
        {
            return Result.Validacion("Lineas", "Debe proporcionar al menos un movimiento de stock.");
        }

        return lineas.Any(linea => linea.ProductoPublicId == Guid.Empty || linea.Cantidad <= 0)
            ? Result.Validacion("Lineas", "Los productos y cantidades del movimiento deben ser válidos.")
            : Result.Exito();
    }
}
