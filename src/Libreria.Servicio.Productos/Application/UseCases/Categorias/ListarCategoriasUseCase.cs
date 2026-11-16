using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Categorias;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Categorias;

public sealed class ListarCategoriasUseCase
{
    private readonly CrudRepositoryFactory<Categoria> _factory;

    public ListarCategoriasUseCase(
        CrudRepositoryFactory<Categoria> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<CategoriaItem>> EjecutarAsync(
        string? busqueda)
    {
        var repository = _factory.CrearRepositorio();

        var categorias =
            await repository.ObtenerActivasAsync(busqueda);

        var resultado = new List<CategoriaItem>();

        foreach (var categoria in categorias)
        {
            var tieneProductos =
                await repository.TieneRelacionesAsync(
                    categoria.PublicId);

            resultado.Add(Mapear(categoria, tieneProductos));
        }

        return resultado;
    }

    private static CategoriaItem Mapear(
        Categoria categoria,
        bool tieneProductos)
    {
        return new CategoriaItem(
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
            tieneProductos);
    }
}