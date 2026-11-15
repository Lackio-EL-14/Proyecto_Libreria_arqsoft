using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Marcas;

public sealed class CambiarEstadoMarcaUseCase
{
    private readonly CrudRepositoryFactory<Marca> _factory;

    public CambiarEstadoMarcaUseCase(
        CrudRepositoryFactory<Marca> factory)
    {
        _factory = factory;
    }

    public async Task<Result> EjecutarAsync(
        Guid publicId,
        bool nuevoEstado)
    {
        var repository = _factory.CrearRepositorio();

        var actual =
            await repository.ObtenerPorPublicIdAsync(
                publicId,
                !nuevoEstado);

        if (actual is null)
        {
            return Result.NoEncontrado(
                "La marca no existe o ya tiene ese estado.");
        }

        if (!nuevoEstado
            && await repository.TieneRelacionesAsync(publicId))
        {
            return Result.Conflicto(
                "Estado",
                "No se puede dar de baja una marca con productos activos.");
        }

        var cambiado =
            await repository.CambiarEstadoAsync(
                publicId,
                nuevoEstado);

        return cambiado
            ? Result.Exito()
            : Result.Conflicto(
                "Estado",
                "No fue posible cambiar el estado de la marca.");
    }
}