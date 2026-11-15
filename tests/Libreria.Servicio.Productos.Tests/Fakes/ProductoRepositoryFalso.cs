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
        if (entidad.PublicId == Guid.Empty)
            entidad.PublicId = Guid.NewGuid();

        entidad.Estado = true;

        if (entidad.FechaCreacion == default)
            entidad.FechaCreacion = DateTime.UtcNow;

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
        if (!string.Equals(
                campo,
                "Nombre",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(false);
        }

        var existe = _productos.Any(producto =>
            (!excluirPublicId.HasValue
                || producto.PublicId != excluirPublicId.Value)
            && string.Equals(
                producto.Nombre,
                valor,
                StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(existe);
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
        var producto = _productos.FirstOrDefault(item =>
            item.PublicId == publicId
            && item.Estado);

        if (producto is null
            || cantidad <= 0
            || producto.Stock < cantidad)
        {
            return Task.FromResult(false);
        }

        producto.Stock -= cantidad;

        return Task.FromResult(true);
    }

    public Task<bool> RestaurarStockAsync(
        Guid publicId,
        int cantidad)
    {
        var producto = _productos.FirstOrDefault(item =>
            item.PublicId == publicId
            && item.Estado);

        if (producto is null || cantidad <= 0)
            return Task.FromResult(false);

        producto.Stock += cantidad;

        return Task.FromResult(true);
    }

    public Task ActualizarCategoriaEmbebidaAsync(
        Guid categoriaPublicId,
        string nuevoNombre)
    {
        foreach (var producto in _productos.Where(producto =>
                    producto.Categoria.PublicId
                        == categoriaPublicId))
        {
            producto.Categoria.Nombre = nuevoNombre;
        }

        return Task.CompletedTask;
    }

    public Task ActualizarMarcaEmbebidaAsync(
        Guid marcaPublicId,
        string nuevoNombre)
    {
        foreach (var producto in _productos.Where(producto =>
                    producto.Marca.PublicId
                        == marcaPublicId))
        {
            producto.Marca.Nombre = nuevoNombre;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Producto>> ObtenerActivasFiltradasAsync(
        string? busqueda,
        Guid? categoriaPublicId,
        Guid? marcaPublicId)
    {
        IEnumerable<Producto> consulta =
            _productos.Where(producto => producto.Estado);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var termino = busqueda.Trim();

            consulta = consulta.Where(producto =>
                producto.Nombre.Contains(
                    termino,
                    StringComparison.OrdinalIgnoreCase)
                || (producto.DescripcionEspecifica?.Contains(
                        termino,
                        StringComparison.OrdinalIgnoreCase)
                    ?? false));
        }

        if (categoriaPublicId.HasValue)
        {
            consulta = consulta.Where(producto =>
                producto.Categoria.PublicId
                    == categoriaPublicId.Value);
        }

        if (marcaPublicId.HasValue)
        {
            consulta = consulta.Where(producto =>
                producto.Marca.PublicId
                    == marcaPublicId.Value);
        }

        IReadOnlyList<Producto> resultado =
            consulta
                .OrderBy(producto => producto.Nombre)
                .ToList();

        return Task.FromResult(resultado);
    }
}