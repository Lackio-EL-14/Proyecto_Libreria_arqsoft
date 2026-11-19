using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class CambiarEstadoProductoUseCase
{
    private readonly IProductoRepository _productos;

    public CambiarEstadoProductoUseCase(
        IProductoRepository productos)
    {
        _productos = productos;
    }

    public async Task<Result> EjecutarAsync(
        Guid publicId,
        bool nuevoEstado)
    {
        var actual =
            await _productos.ObtenerPorPublicIdAsync(
                publicId,
                !nuevoEstado);

        if (actual is null)
        {
            return Result.NoEncontrado(
                "El producto no existe o ya tiene ese estado.");
        }

        var cambiado =
            await _productos.CambiarEstadoAsync(
                publicId,
                nuevoEstado);

        return cambiado
            ? Result.Exito()
            : Result.Conflicto(
                "Estado",
                "No fue posible cambiar el estado del producto.");
    }
}