using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Tests.Fakes;

public sealed class CatalogoProductoRepositoryFalso
    : ICatalogoProductoRepository
{
    private readonly List<Categoria> _categorias = [];
    private readonly List<Marca> _marcas = [];

    public void AgregarCategoria(Categoria categoria)
        => _categorias.Add(categoria);

    public void AgregarMarca(Marca marca)
        => _marcas.Add(marca);

    public Task<IReadOnlyList<Categoria>>
        ObtenerCategoriasActivasAsync()
    {
        IReadOnlyList<Categoria> resultado =
            _categorias
                .Where(item => item.Estado)
                .ToList();

        return Task.FromResult(resultado);
    }

    public Task<IReadOnlyList<Marca>>
        ObtenerMarcasActivasAsync()
    {
        IReadOnlyList<Marca> resultado =
            _marcas
                .Where(item => item.Estado)
                .ToList();

        return Task.FromResult(resultado);
    }

    public Task<Categoria?> ObtenerCategoriaActivaAsync(
        Guid publicId)
    {
        var resultado = _categorias.FirstOrDefault(item =>
            item.PublicId == publicId
            && item.Estado);

        return Task.FromResult(resultado);
    }

    public Task<Marca?> ObtenerMarcaActivaAsync(
        Guid publicId)
    {
        var resultado = _marcas.FirstOrDefault(item =>
            item.PublicId == publicId
            && item.Estado);

        return Task.FromResult(resultado);
    }

    public Task<bool> ExisteCategoriaActivaAsync(
        Guid publicId)
    {
        return Task.FromResult(
            _categorias.Any(item =>
                item.PublicId == publicId
                && item.Estado));
    }

    public Task<bool> ExisteMarcaActivaAsync(
        Guid publicId)
    {
        return Task.FromResult(
            _marcas.Any(item =>
                item.PublicId == publicId
                && item.Estado));
    }
}