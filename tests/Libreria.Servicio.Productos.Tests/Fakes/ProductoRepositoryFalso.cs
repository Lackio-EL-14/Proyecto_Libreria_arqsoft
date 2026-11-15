using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain.Entities;

namespace Libreria.Servicio.Productos.Tests.Fakes;

public sealed class ProductoRepositoryFalso : IProductoRepository
{
    private readonly List<Producto> _productos = [];

    public void Agregar(Producto producto)
    {
        _productos.Add(producto);
    }

    public Task<IReadOnlyList<Producto>> ObtenerActivasAsync(
        string? busqueda = null)
    {
        IEnumerable<Producto> consulta =
            _productos.Where(producto => producto.Estado);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            consulta = consulta.Where(producto =>
                producto.Nombre.Contains(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase)
                || (producto.DescripcionEspecifica?.Contains(
                    busqueda,
                    StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return Task.FromResult<IReadOnlyList<Producto>>(
            consulta.ToList());
    }

    public Task<Producto?> ObtenerPorPublicIdAsync(
        Guid publicId,
        bool estadoEsperado)
    {
        return Task.FromResult(
            _productos.FirstOrDefault(producto =>
                producto.PublicId == publicId
                && producto.Estado == estadoEsperado));
    }

    public Task CrearAsync(Producto entidad)
    {
        _productos.Add(entidad);
        return Task.CompletedTask;
    }

    public Task<bool> ActualizarAsync(Producto entidad)
    {
        return Task.FromResult(true);
    }

    public Task<bool> CambiarEstadoAsync(
        Guid publicId,
        bool nuevoEstado)
    {
        var producto = _productos.FirstOrDefault(
            item => item.PublicId == publicId);

        if (producto is null)
            return Task.FromResult(false);

        producto.Estado = nuevoEstado;
        return Task.FromResult(true);
    }

    public Task<bool> ExisteValorAsync(
        string campo,
        string valor,
        Guid? excluirPublicId = null)
    {
        return Task.FromResult(false);
    }

    public Task<bool> TieneRelacionesAsync(Guid publicId)
    {
        return Task.FromResult(false);
    }

    public Task<Producto?> ObtenerParaVentaAsync(Guid publicId)
    {
        return ObtenerPorPublicIdAsync(publicId, true);
    }

    public Task<bool> DescontarStockAsync(
        Guid publicId,
        int cantidad)
    {
        return Task.FromResult(false);
    }

    public Task<bool> RestaurarStockAsync(
        Guid publicId,
        int cantidad)
    {
        return Task.FromResult(false);
    }

    public Task ActualizarCategoriaEmbebidaAsync(
        Guid categoriaPublicId,
        string nuevoNombre)
    {
        return Task.CompletedTask;
    }

    public Task ActualizarMarcaEmbebidaAsync(
        Guid marcaPublicId,
        string nuevoNombre)
    {
        return Task.CompletedTask;
    }
}