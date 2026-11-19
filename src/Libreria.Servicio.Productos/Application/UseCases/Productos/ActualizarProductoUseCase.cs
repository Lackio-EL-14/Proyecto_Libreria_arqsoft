using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain;
using Libreria.Servicio.Productos.Application.Models.Productos;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class ActualizarProductoUseCase
{
    private readonly IProductoRepository _productos;
    private readonly ICatalogoProductoRepository _catalogos;
    private readonly ProductoValidator _validator;

    public ActualizarProductoUseCase(
        IProductoRepository productos,
        ICatalogoProductoRepository catalogos,
        ProductoValidator validator)
    {
        _productos = productos;
        _catalogos = catalogos;
        _validator = validator;
    }

    public async Task<Result> EjecutarAsync(
        Guid publicId,
        ProductoInput input)
    {
        var actual =
            await _productos.ObtenerPorPublicIdAsync(
                publicId,
                true);

        if (actual is null)
        {
            return Result.NoEncontrado(
                "El producto no existe o está inactivo.");
        }

        var validacion =
            await _validator.ValidarAsync(
                input,
                publicId);

        if (!validacion.EsExito)
            return validacion;

        var categoria =
            await _catalogos.ObtenerCategoriaActivaAsync(
                input.CategoriaPublicId);

        var marca =
            await _catalogos.ObtenerMarcaActivaAsync(
                input.MarcaPublicId);

        if (categoria is null)
        {
            return Result.Validacion(
                "CategoriaPublicId",
                "La categoría seleccionada no existe o está inactiva.");
        }

        if (marca is null)
        {
            return Result.Validacion(
                "MarcaPublicId",
                "La marca seleccionada no existe o está inactiva.");
        }

        actual.Nombre = input.Nombre;
        actual.DescripcionEspecifica =
            input.DescripcionEspecifica;
        actual.EsPerecedero = input.EsPerecedero;
        actual.FechaVencimiento =
            input.FechaVencimiento;
        actual.Stock = input.Stock;
        actual.PrecioVenta = input.PrecioVenta;

        // Si cambia, MongoProductoRepository hará:
        // $set del costo + $push del histórico
        // en una única actualización.
        actual.CostoAdquisicionActual =
            input.CostoAdquisicion;

        actual.Categoria = new ReferenciaCatalogo
        {
            PublicId = categoria.PublicId,
            Nombre = categoria.Nombre
        };

        actual.Marca = new ReferenciaCatalogo
        {
            PublicId = marca.PublicId,
            Nombre = marca.Nombre
        };

        var actualizado =
            await _productos.ActualizarAsync(actual);

        return actualizado
            ? Result.Exito()
            : Result.Conflicto(
                string.Empty,
                "No fue posible actualizar el producto.");
    }
}