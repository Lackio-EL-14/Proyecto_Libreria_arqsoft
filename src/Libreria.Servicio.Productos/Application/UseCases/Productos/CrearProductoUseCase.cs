using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Models.Productos;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class CrearProductoUseCase
{
    private readonly IProductoRepository _productos;
    private readonly ICatalogoProductoRepository _catalogos;
    private readonly ProductoValidator _validator;

    public CrearProductoUseCase(
        IProductoRepository productos,
        ICatalogoProductoRepository catalogos,
        ProductoValidator validator)
    {
        _productos = productos;
        _catalogos = catalogos;
        _validator = validator;
    }

    public async Task<Result<Guid>> EjecutarAsync(
        ProductoInput input)
    {
        var validacion =
            await _validator.ValidarAsync(input);

        if (!validacion.EsExito)
            return Result<Guid>.Fallo(validacion);

        var categoria =
            await _catalogos.ObtenerCategoriaActivaAsync(
                input.CategoriaPublicId);

        var marca =
            await _catalogos.ObtenerMarcaActivaAsync(
                input.MarcaPublicId);

        if (categoria is null)
        {
            return Result<Guid>.Fallo(
                Result.Validacion(
                    "CategoriaPublicId",
                    "La categoría seleccionada no existe o está inactiva."));
        }

        if (marca is null)
        {
            return Result<Guid>.Fallo(
                Result.Validacion(
                    "MarcaPublicId",
                    "La marca seleccionada no existe o está inactiva."));
        }

        var producto = new Producto
        {
            Nombre = input.Nombre,
            DescripcionEspecifica = input.DescripcionEspecifica,
            EsPerecedero = input.EsPerecedero,
            FechaVencimiento = input.FechaVencimiento,
            Stock = input.Stock,
            PrecioVenta = input.PrecioVenta,
            CostoAdquisicionActual = input.CostoAdquisicion,

            Categoria = new ReferenciaCatalogo
            {
                PublicId = categoria.PublicId,
                Nombre = categoria.Nombre
            },

            Marca = new ReferenciaCatalogo
            {
                PublicId = marca.PublicId,
                Nombre = marca.Nombre
            }
        };

        await _productos.CrearAsync(producto);

        return Result<Guid>.Exito(producto.PublicId);
    }
}