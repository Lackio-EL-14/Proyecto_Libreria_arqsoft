using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Categorias;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Categorias;

public sealed class CrearCategoriaUseCase
{
    private readonly CrudRepositoryFactory<Categoria> _factory;
    private readonly CategoriaValidator _validator;

    public CrearCategoriaUseCase(
        CrudRepositoryFactory<Categoria> factory,
        CategoriaValidator validator)
    {
        _factory = factory;
        _validator = validator;
    }

    public async Task<Result<Guid>> EjecutarAsync(
        CategoriaInput input)
    {
        var validacion =
            await _validator.ValidarAsync(input);

        if (!validacion.EsExito)
        {
            return Result<Guid>.Fallo(validacion);
        }

        var categoria = new Categoria
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            Ubicacion = input.Ubicacion
        };

        var repository = _factory.CrearRepositorio();

        await repository.CrearAsync(categoria);

        return Result<Guid>.Exito(categoria.PublicId);
    }
}