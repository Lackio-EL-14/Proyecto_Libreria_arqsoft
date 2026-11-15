using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.UseCases.Productos;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Tests.Fakes;

namespace Libreria.Servicio.Productos.Tests.Productos;

public sealed class InventarioUseCaseTests
{
    [Fact]
    public async Task Buscar_devuelve_productos_activos_que_coinciden()
    {
        var repository = new ProductoRepositoryFalso();

        repository.Agregar(CrearProducto(
            "Cuaderno universitario",
            stock: 20));

        repository.Agregar(CrearProducto(
            "Calculadora científica",
            stock: 10));

        var useCase = new BuscarProductosUseCase(repository);

        var resultado = await useCase.EjecutarAsync("cuaderno");

        Assert.True(resultado.EsExito);

        var producto = Assert.Single(resultado.Valor);

        Assert.Equal("Cuaderno universitario", producto.Nombre);
        Assert.Equal(20, producto.Stock);
        Assert.Equal(15m, producto.PrecioVenta);
    }

    [Fact]
    public async Task Buscar_sin_termino_devuelve_validacion()
    {
        var repository = new ProductoRepositoryFalso();
        var useCase = new BuscarProductosUseCase(repository);

        var resultado = await useCase.EjecutarAsync("   ");

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("Termino", resultado.Errores.Keys);
    }

    [Fact]
    public async Task Descontar_stock_valido_reduce_existencia()
    {
        var repository = new ProductoRepositoryFalso();

        var producto = CrearProducto(
            "Cuaderno",
            stock: 10);

        repository.Agregar(producto);

        var useCase = new DescontarStockUseCase(repository);

        var resultado = await useCase.EjecutarAsync(
            producto.PublicId,
            3,
            Guid.NewGuid());

        Assert.True(resultado.EsExito);

        var actualizado =
            await repository.ObtenerParaVentaAsync(
                producto.PublicId);

        Assert.NotNull(actualizado);
        Assert.Equal(7, actualizado.Stock);
    }

    [Fact]
    public async Task Descontar_stock_insuficiente_devuelve_conflicto_y_no_modifica()
    {
        var repository = new ProductoRepositoryFalso();

        var producto = CrearProducto(
            "Cuaderno",
            stock: 5);

        repository.Agregar(producto);

        var useCase = new DescontarStockUseCase(repository);

        var resultado = await useCase.EjecutarAsync(
            producto.PublicId,
            8,
            Guid.NewGuid());

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);

        var actualizado =
            await repository.ObtenerParaVentaAsync(
                producto.PublicId);

        Assert.NotNull(actualizado);
        Assert.Equal(5, actualizado.Stock);
    }

    [Fact]
    public async Task Restaurar_stock_valido_incrementa_existencia()
    {
        var repository = new ProductoRepositoryFalso();

        var producto = CrearProducto(
            "Cuaderno",
            stock: 7);

        repository.Agregar(producto);

        var useCase = new RestaurarStockUseCase(repository);

        var resultado = await useCase.EjecutarAsync(
            producto.PublicId,
            3,
            Guid.NewGuid());

        Assert.True(resultado.EsExito);

        var actualizado =
            await repository.ObtenerParaVentaAsync(
                producto.PublicId);

        Assert.NotNull(actualizado);
        Assert.Equal(10, actualizado.Stock);
    }

    [Fact]
    public async Task Cantidad_no_valida_devuelve_validacion()
    {
        var repository = new ProductoRepositoryFalso();

        var producto = CrearProducto(
            "Cuaderno",
            stock: 10);

        repository.Agregar(producto);

        var useCase = new DescontarStockUseCase(repository);

        var resultado = await useCase.EjecutarAsync(
            producto.PublicId,
            0,
            Guid.NewGuid());

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("Cantidad", resultado.Errores.Keys);
    }

    private static Producto CrearProducto(
        string nombre,
        int stock)
    {
        return new Producto
        {
            PublicId = Guid.NewGuid(),
            Nombre = nombre,
            Stock = stock,
            PrecioVenta = 15m,
            CostoAdquisicionActual = 10m,
            Estado = true,
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