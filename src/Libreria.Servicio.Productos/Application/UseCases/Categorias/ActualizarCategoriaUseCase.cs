using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Categorias;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Categorias;

public sealed class ActualizarCategoriaUseCase
{
    private readonly CrudRepositoryFactory<Categoria> _factory;
    private readonly CategoriaValidator _validator;
    private readonly IProductoRepository _productos;

    public ActualizarCategoriaUseCase(
        CrudRepositoryFactory<Categoria> factory,
        CategoriaValidator validator,
        IProductoRepository productos)
    {
        _factory = factory;
        _validator = validator;
        _productos = productos;
    }

    public async Task<Result> EjecutarAsync(
        Guid publicId,
        CategoriaInput input)
    {
        var repository = _factory.CrearRepositorio();

        var actual =
            await repository.ObtenerPorPublicIdAsync(
                publicId,
                true);

        if (actual is null)
        {
            return Result.NoEncontrado(
                "La categoría no existe o está inactiva.");
        }

        var validacion =
            await _validator.ValidarAsync(
                input,
                publicId);

        if (!validacion.EsExito)
            return validacion;

        var cambioNombre = !string.Equals(
            actual.Nombre,
            input.Nombre,
            StringComparison.Ordinal);

        actual.Nombre = input.Nombre;
        actual.Descripcion = input.Descripcion;
        actual.Ubicacion = input.Ubicacion;

        var actualizada =
            await repository.ActualizarAsync(actual);

        if (!actualizada)
        {
            return Result.Conflicto(
                string.Empty,
                "No fue posible actualizar la categoría.");
        }

        if (cambioNombre)
        {
            await _productos.ActualizarCategoriaEmbebidaAsync(
                publicId,
                input.Nombre);
        }

        return Result.Exito();
    }
}