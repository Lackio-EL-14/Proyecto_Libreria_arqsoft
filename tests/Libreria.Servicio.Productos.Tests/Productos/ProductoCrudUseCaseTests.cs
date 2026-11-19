using Libreria.Servicio.Productos.Domain;
using Libreria.Servicio.Productos.Application.Models.Productos;
using Libreria.Servicio.Productos.Application.UseCases.Productos;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Tests.Fakes;

namespace Libreria.Servicio.Productos.Tests.Productos;

public sealed class ProductoCrudUseCaseTests
{
    [Fact]
    public async Task Validar_producto_invalido_devuelve_errores_de_negocio()
    {
        var productos = new ProductoRepositoryFalso();
        var catalogos = new CatalogoProductoRepositoryFalso();

        var validator =
            new ProductoValidator(productos, catalogos);

        var input = new ProductoInput
        {
            Nombre = "",
            EsPerecedero = true,
            FechaVencimiento = null,
            Stock = -1,
            PrecioVenta = -10,
            CostoAdquisicion = -5,
            CategoriaPublicId = Guid.Empty,
            MarcaPublicId = Guid.Empty
        };

        var resultado =
            await validator.ValidarAsync(input);

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);

        Assert.Contains("Nombre", resultado.Errores.Keys);
        Assert.Contains("Stock", resultado.Errores.Keys);
        Assert.Contains("PrecioVenta", resultado.Errores.Keys);
        Assert.Contains("CostoAdquisicion", resultado.Errores.Keys);
        Assert.Contains("FechaVencimiento", resultado.Errores.Keys);
        Assert.Contains("CategoriaPublicId", resultado.Errores.Keys);
        Assert.Contains("MarcaPublicId", resultado.Errores.Keys);
    }

    [Fact]
    public async Task Validar_nombre_duplicado_devuelve_validacion()
    {
        var productos = new ProductoRepositoryFalso();
        var catalogos = CrearCatalogosValidos(
            out var categoria,
            out var marca);

        productos.Agregar(
            CrearProducto(
                "Producto duplicado",
                categoria,
                marca));

        var validator =
            new ProductoValidator(productos, catalogos);

        var input = CrearInput(
            "Producto duplicado",
            categoria.PublicId,
            marca.PublicId);

        var resultado =
            await validator.ValidarAsync(input);

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("Nombre", resultado.Errores.Keys);
    }

    [Fact]
    public async Task Crear_producto_valido_guarda_referencias_embebidas()
    {
        var productos = new ProductoRepositoryFalso();

        var catalogos = CrearCatalogosValidos(
            out var categoria,
            out var marca);

        var validator =
            new ProductoValidator(productos, catalogos);

        var useCase =
            new CrearProductoUseCase(
                productos,
                catalogos,
                validator);

        var input = CrearInput(
            "Producto nuevo",
            categoria.PublicId,
            marca.PublicId);

        var resultado =
            await useCase.EjecutarAsync(input);

        Assert.True(resultado.EsExito);
        Assert.NotEqual(Guid.Empty, resultado.Valor);

        var creado =
            await productos.ObtenerPorPublicIdAsync(
                resultado.Valor,
                true);

        Assert.NotNull(creado);

        Assert.Equal(
            categoria.PublicId,
            creado.Categoria.PublicId);

        Assert.Equal(
            "Arte",
            creado.Categoria.Nombre);

        Assert.Equal(
            marca.PublicId,
            creado.Marca.PublicId);

        Assert.Equal(
            "Marca prueba",
            creado.Marca.Nombre);
    }

    [Fact]
    public async Task Actualizar_producto_valido_cambia_datos()
    {
        var productos = new ProductoRepositoryFalso();

        var catalogos = CrearCatalogosValidos(
            out var categoria,
            out var marca);

        var producto =
            CrearProducto(
                "Producto original",
                categoria,
                marca);

        productos.Agregar(producto);

        var validator =
            new ProductoValidator(productos, catalogos);

        var useCase =
            new ActualizarProductoUseCase(
                productos,
                catalogos,
                validator);

        var input = CrearInput(
            "Producto actualizado",
            categoria.PublicId,
            marca.PublicId);

        input.Stock = 25;
        input.PrecioVenta = 30m;
        input.CostoAdquisicion = 20m;

        var resultado =
            await useCase.EjecutarAsync(
                producto.PublicId,
                input);

        Assert.True(resultado.EsExito);

        var actualizado =
            await productos.ObtenerPorPublicIdAsync(
                producto.PublicId,
                true);

        Assert.NotNull(actualizado);

        Assert.Equal(
            "Producto actualizado",
            actualizado.Nombre);

        Assert.Equal(25, actualizado.Stock);
        Assert.Equal(30m, actualizado.PrecioVenta);
        Assert.Equal(
            20m,
            actualizado.CostoAdquisicionActual);
    }

    private static CatalogoProductoRepositoryFalso
        CrearCatalogosValidos(
            out Categoria categoria,
            out Marca marca)
    {
        var catalogos =
            new CatalogoProductoRepositoryFalso();

        categoria = new Categoria
        {
            PublicId = Guid.NewGuid(),
            Codigo = "CAT-TEST",
            Nombre = "Arte",
            Ubicacion = "Vitrina",
            Estado = true
        };

        marca = new Marca
        {
            PublicId = Guid.NewGuid(),
            Nombre = "Marca prueba",
            PaisOrigen = "Bolivia",
            Estado = true
        };

        catalogos.AgregarCategoria(categoria);
        catalogos.AgregarMarca(marca);

        return catalogos;
    }

    private static ProductoInput CrearInput(
        string nombre,
        Guid categoriaPublicId,
        Guid marcaPublicId)
    {
        return new ProductoInput
        {
            Nombre = nombre,
            DescripcionEspecifica = "Prueba",
            EsPerecedero = false,
            FechaVencimiento = null,
            Stock = 10,
            PrecioVenta = 15m,
            CostoAdquisicion = 10m,
            CategoriaPublicId = categoriaPublicId,
            MarcaPublicId = marcaPublicId
        };
    }

    private static Producto CrearProducto(
        string nombre,
        Categoria categoria,
        Marca marca)
    {
        return new Producto
        {
            PublicId = Guid.NewGuid(),
            Nombre = nombre,
            Stock = 10,
            PrecioVenta = 15m,
            CostoAdquisicionActual = 10m,
            Estado = true,

            Categoria = new ReferenciaCatalogo
            {
                PublicId = categoria.PublicId,
                Nombre = categoria.Nombre
            },

            Marca = new ReferenciaCatalogo
            {
                PublicId = marca.PublicId,
                Nombre = marca.Nombre
            },

            FechaCreacion = DateTime.UtcNow
        };
    }
}