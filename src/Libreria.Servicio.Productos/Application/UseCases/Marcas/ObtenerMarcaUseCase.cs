using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Marcas;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Application.UseCases.Marcas;

public sealed class ObtenerMarcaUseCase
{
    private readonly CrudRepositoryFactory<Marca> _factory;

    public ObtenerMarcaUseCase(
        CrudRepositoryFactory<Marca> factory)
    {
        _factory = factory;
    }

    public async Task<Result<MarcaItem>> EjecutarAsync(
        Guid publicId,
        bool estadoEsperado)
    {
        var repository = _factory.CrearRepositorio();

        var marca =
            await repository.ObtenerPorPublicIdAsync(
                publicId,
                estadoEsperado);

        if (marca is null)
        {
            return Result<MarcaItem>.Fallo(
                Result.NoEncontrado(
                    "La marca no existe."));
        }

        var tieneProductos =
            await repository.TieneRelacionesAsync(publicId);

        return Result<MarcaItem>.Exito(
            new MarcaItem(
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
                tieneProductos));
    }
}