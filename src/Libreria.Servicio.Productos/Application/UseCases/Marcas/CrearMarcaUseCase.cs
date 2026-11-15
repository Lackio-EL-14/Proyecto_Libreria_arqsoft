using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Marcas;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Marcas;

public sealed class CrearMarcaUseCase
{
    private readonly CrudRepositoryFactory<Marca> _factory;
    private readonly MarcaValidator _validator;

    public CrearMarcaUseCase(
        CrudRepositoryFactory<Marca> factory,
        MarcaValidator validator)
    {
        _factory = factory;
        _validator = validator;
    }

    public async Task<Result<Guid>> EjecutarAsync(
        MarcaInput input)
    {
        var validacion =
            await _validator.ValidarAsync(input);

        if (!validacion.EsExito)
            return Result<Guid>.Fallo(validacion);

        var marca = new Marca
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            PaisOrigen = input.PaisOrigen,
            SitioWeb = input.SitioWeb
        };

        var repository = _factory.CrearRepositorio();

        await repository.CrearAsync(marca);

        return Result<Guid>.Exito(marca.PublicId);
    }
}