using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Categorias;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Categorias;

public sealed class ObtenerCategoriaUseCase
{
    private readonly CrudRepositoryFactory<Categoria> _factory;

    public ObtenerCategoriaUseCase(
        CrudRepositoryFactory<Categoria> factory)
    {
        _factory = factory;
    }

    public async Task<Result<CategoriaItem>> EjecutarAsync(
        Guid publicId,
        bool estadoEsperado)
    {
        var repository = _factory.CrearRepositorio();

        var categoria =
            await repository.ObtenerPorPublicIdAsync(
                publicId,
                estadoEsperado);

        if (categoria is null)
        {
            return Result<CategoriaItem>.Fallo(
                Result.NoEncontrado(
                    "La categoría no existe."));
        }

        var tieneProductos =
            await repository.TieneRelacionesAsync(publicId);

        return Result<CategoriaItem>.Exito(
            new CategoriaItem(
                categoria.PublicId,
                categoria.Codigo,
                categoria.Nombre,
                categoria.Descripcion,
                categoria.Ubicacion,
                categoria.Estado,
                categoria.UsuarioCreacionId,
                categoria.FechaCreacion,
                categoria.UsuarioModificacionId,
                categoria.FechaModificacion,
                tieneProductos));
    }
}