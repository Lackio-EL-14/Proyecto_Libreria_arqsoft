using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Models.Productos;

namespace Libreria.Servicio.Productos.Application.UseCases.Productos;

public sealed class BuscarProductosUseCase
{
    private readonly IProductoRepository _repository;

    public BuscarProductosUseCase(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ProductoVentaItem>>> EjecutarAsync(
        string? termino)
    {
        if (string.IsNullOrWhiteSpace(termino))
        {
            return Result<IReadOnlyList<ProductoVentaItem>>.Fallo(
                Result.Validacion(
                    "Termino",
                    "Ingrese un término para buscar productos."));
        }

        var productos = await _repository.ObtenerActivasAsync(
            termino.Trim());

        var resultado = productos
            .Select(producto => new ProductoVentaItem(
                producto.PublicId,
                producto.Nombre,
                producto.Stock,
                producto.PrecioVenta))
            .ToList();

        return Result<IReadOnlyList<ProductoVentaItem>>.Exito(resultado);
    }
}