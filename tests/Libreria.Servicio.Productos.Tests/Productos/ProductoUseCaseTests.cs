using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.UseCases.Productos;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Tests.Fakes;

namespace Libreria.Servicio.Productos.Tests.Productos;

public sealed class ProductoUseCaseTests
{
    [Fact]
    public async Task Listado_solo_devuelve_productos_activos()
    {
        var repository = new ProductoRepositoryFalso();

        repository.Agregar(CrearProducto(
            "Cuaderno",
            estado: true));

        repository.Agregar(CrearProducto(
            "Producto inactivo",
            estado: false));

       var catalogos =
        new CatalogoProductoRepositoryFalso();

        var useCase =
            new ObtenerListadoProductosUseCase(
                repository,
                catalogos);

        var resultado = await useCase.EjecutarAsync();

        var producto = Assert.Single(resultado.Productos);
        Assert.Equal("Cuaderno", producto.Nombre);
    }

    [Fact]
    public async Task Listado_puede_filtrar_por_nombre()
    {
        var repository = new ProductoRepositoryFalso();

        repository.Agregar(CrearProducto("Cuaderno Espiral"));
        repository.Agregar(CrearProducto("Calculadora Científica"));

       var catalogos =
            new CatalogoProductoRepositoryFalso();

        var useCase =
            new ObtenerListadoProductosUseCase(
                repository,
                catalogos);

        var resultado = await useCase.EjecutarAsync("cuaderno");

        var producto = Assert.Single(resultado.Productos);
        Assert.Equal("Cuaderno Espiral", producto.Nombre);
    }

    [Fact]
    public async Task Detalle_devuelve_producto_existente()
    {
        var repository = new ProductoRepositoryFalso();
        var producto = CrearProducto("Cuaderno");

        repository.Agregar(producto);

        var useCase =
            new ObtenerDetalleProductoUseCase(repository);

        var resultado =
            await useCase.EjecutarAsync(producto.PublicId);

        Assert.True(resultado.EsExito);
        Assert.Equal(
            producto.PublicId,
            resultado.Valor.PublicId);
        Assert.Equal(
            "Material escolar",
            resultado.Valor.Categoria);
    }

    [Fact]
    public async Task Detalle_inexistente_devuelve_no_encontrado()
    {
        var repository = new ProductoRepositoryFalso();

        var useCase =
            new ObtenerDetalleProductoUseCase(repository);

        var resultado =
            await useCase.EjecutarAsync(Guid.NewGuid());

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    private static Producto CrearProducto(
        string nombre,
        bool estado = true)
    {
        return new Producto
        {
            PublicId = Guid.NewGuid(),
            Nombre = nombre,
            Stock = 10,
            PrecioVenta = 15m,
            CostoAdquisicionActual = 10m,
            Estado = estado,
            Categoria = new ReferenciaCatalogo
            {
                PublicId = Guid.NewGuid(),
                Nombre = "Material escolar"
            },
            Marca = new ReferenciaCatalogo
            {
                PublicId = Guid.NewGuid(),
                Nombre = "Faber-Castell"
            },
            FechaCreacion = DateTime.UtcNow
        };
    }
}