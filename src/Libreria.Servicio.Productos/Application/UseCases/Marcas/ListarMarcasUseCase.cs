using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Marcas;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Marcas;

public sealed class ListarMarcasUseCase
{
    private readonly CrudRepositoryFactory<Marca> _factory;

    public ListarMarcasUseCase(
        CrudRepositoryFactory<Marca> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<MarcaItem>> EjecutarAsync(
        string? busqueda)
    {
        var repository = _factory.CrearRepositorio();

        var marcas =
            await repository.ObtenerActivasAsync(busqueda);

        var resultado = new List<MarcaItem>();

        foreach (var marca in marcas)
        {
            var tieneProductos =
                await repository.TieneRelacionesAsync(
                    marca.PublicId);

            resultado.Add(Mapear(marca, tieneProductos));
        }

        return resultado;
    }

    private static MarcaItem Mapear(
        Marca marca,
        bool tieneProductos)
    {
        return new MarcaItem(
            marca.PublicId,
            marca.Nombre,
            marca.Descripcion,
            marca.PaisOrigen,
            marca.SitioWeb,
            marca.Estado,
            marca.UsuarioCreacionId,
            marca.FechaCreacion,
            marca.UsuarioModificacionId,
            marca.FechaModificacion,
            tieneProductos);
    }
}