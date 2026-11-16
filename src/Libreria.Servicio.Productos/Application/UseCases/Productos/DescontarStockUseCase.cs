using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class DescontarStockUseCase
{
    private readonly IProductoRepository _repository;

    public DescontarStockUseCase(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> EjecutarAsync(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId)
    {
        var validacion = Validar(
            productoPublicId,
            cantidad,
            ventaPublicId);

        if (!validacion.EsExito)
            return validacion;

        var producto = await _repository.ObtenerParaVentaAsync(
            productoPublicId);

        if (producto is null)
        {
            return Result.NoEncontrado(
                "El producto no existe o está inactivo.");
        }

        if (producto.Stock < cantidad)
        {
            return Result.Conflicto(
                "Stock",
                "No existe stock suficiente.");
        }

        // La condición stock >= cantidad también se verifica dentro
        // del UpdateOne de Mongo. Así evitamos una carrera entre
        // esta lectura y la actualización.
        var descontado = await _repository.DescontarStockAsync(
            productoPublicId,
            cantidad);

        return descontado
            ? Result.Exito()
            : Result.Conflicto(
                "Stock",
                "No existe stock suficiente.");
    }

    private static Result Validar(
        Guid productoPublicId,
        int cantidad,
        Guid ventaPublicId)
    {
        if (productoPublicId == Guid.Empty)
        {
            return Result.Validacion(
                "ProductoPublicId",
                "El identificador del producto es obligatorio.");
        }

        if (cantidad <= 0)
        {
            return Result.Validacion(
                "Cantidad",
                "La cantidad debe ser mayor que cero.");
        }

        if (ventaPublicId == Guid.Empty)
        {
            return Result.Validacion(
                "VentaPublicId",
                "El identificador de la venta es obligatorio.");
        }

        return Result.Exito();
    }
}