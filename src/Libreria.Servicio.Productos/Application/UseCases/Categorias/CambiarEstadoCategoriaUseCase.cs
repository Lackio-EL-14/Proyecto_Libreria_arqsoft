using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Categorias;

public sealed class CambiarEstadoCategoriaUseCase
{
    private readonly CrudRepositoryFactory<Categoria> _factory;

    public CambiarEstadoCategoriaUseCase(
        CrudRepositoryFactory<Categoria> factory)
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
                "La categoría no existe o ya tiene ese estado.");
        }

        if (!nuevoEstado
            && await repository.TieneRelacionesAsync(publicId))
        {
            return Result.Conflicto(
                "Estado",
                "No se puede dar de baja una categoría con productos activos.");
        }

        var cambiado =
            await repository.CambiarEstadoAsync(
                publicId,
                nuevoEstado);

        return cambiado
            ? Result.Exito()
            : Result.Conflicto(
                "Estado",
                "No fue posible cambiar el estado de la categoría.");
    }
}