using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain;
using Libreria.Servicio.Productos.Application.Models.Productos;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class ObtenerDetalleProductoUseCase
{
    private readonly IProductoRepository _repository;

    public ObtenerDetalleProductoUseCase(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductoDetalle>> EjecutarAsync(Guid publicId)
    {
        var producto = await _repository.ObtenerPorPublicIdAsync(
            publicId,
            true);

        if (producto is null)
        {
            return Result<ProductoDetalle>.Fallo(
                Result.NoEncontrado("El producto no existe."));
        }

        return Result<ProductoDetalle>.Exito(
            new ProductoDetalle(
                producto.PublicId,
                producto.Nombre,
                producto.DescripcionEspecifica,
                producto.EsPerecedero,
                producto.FechaVencimiento,
                producto.Stock,
                producto.PrecioVenta,
                producto.CostoAdquisicionActual,
                producto.Categoria.PublicId,
                producto.Categoria.Nombre,
                producto.Marca.PublicId,
                producto.Marca.Nombre,
                producto.UsuarioCreacionId,
                producto.FechaCreacion,
                producto.UsuarioModificacionId,
                producto.FechaModificacion));
    }
}