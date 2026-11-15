using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class RestaurarStockUseCase
{
    private readonly IProductoRepository _repository;

    public RestaurarStockUseCase(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> EjecutarAsync(
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

        var producto = await _repository.ObtenerParaVentaAsync(
            productoPublicId);

        if (producto is null)
        {
            return Result.NoEncontrado(
                "El producto no existe o está inactivo.");
        }

        var restaurado = await _repository.RestaurarStockAsync(
            productoPublicId,
            cantidad);

        return restaurado
            ? Result.Exito()
            : Result.Conflicto(
                "Stock",
                "No fue posible restaurar el stock del producto.");
    }
}