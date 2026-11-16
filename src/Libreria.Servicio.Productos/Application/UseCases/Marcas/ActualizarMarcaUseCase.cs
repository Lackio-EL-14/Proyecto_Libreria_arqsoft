using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Marcas;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Marcas;

public sealed class ActualizarMarcaUseCase
{
    private readonly CrudRepositoryFactory<Marca> _factory;
    private readonly MarcaValidator _validator;
    private readonly IProductoRepository _productos;

    public ActualizarMarcaUseCase(
        CrudRepositoryFactory<Marca> factory,
        MarcaValidator validator,
        IProductoRepository productos)
    {
        _factory = factory;
        _validator = validator;
        _productos = productos;
    }

    public async Task<Result> EjecutarAsync(
        Guid publicId,
        MarcaInput input)
    {
        var repository = _factory.CrearRepositorio();

        var actual =
            await repository.ObtenerPorPublicIdAsync(
                publicId,
                true);

        if (actual is null)
        {
            return Result.NoEncontrado(
                "La marca no existe o está inactiva.");
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
        actual.PaisOrigen = input.PaisOrigen;
        actual.SitioWeb = input.SitioWeb;

        var actualizada =
            await repository.ActualizarAsync(actual);

        if (!actualizada)
        {
            return Result.Conflicto(
                string.Empty,
                "No fue posible actualizar la marca.");
        }

        if (cambioNombre)
        {
            await _productos.ActualizarMarcaEmbebidaAsync(
                publicId,
                input.Nombre);
        }

        return Result.Exito();
    }
}