using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Domain;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Categorias;
using Libreria.Servicio.Productos.Application.Models.Marcas;
using Libreria.Servicio.Productos.Application.UseCases.Categorias;
using Libreria.Servicio.Productos.Application.UseCases.Marcas;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Tests.Fakes;

namespace Libreria.Servicio.Productos.Tests.Productos;

public sealed class CatalogosUseCaseTests
{
    [Fact]
    public async Task Categoria_nombre_duplicado_devuelve_validacion()
    {
        var repository = new CategoriaRepositoryFalso();

        repository.Agregar(new Categoria
        {
            PublicId = Guid.NewGuid(),
            Codigo = "CAT-001",
            Nombre = "Arte",
            Ubicacion = "Bodega",
            Estado = true
        });

        var validator = new CategoriaValidator(repository);

        var resultado = await validator.ValidarAsync(
            new CategoriaInput
            {
                Nombre = "Arte",
                Ubicacion = "Bodega"
            });

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("Nombre", resultado.Errores.Keys);
    }

    [Fact]
    public async Task Categoria_ubicacion_invalida_devuelve_validacion()
    {
        var repository = new CategoriaRepositoryFalso();
        var validator = new CategoriaValidator(repository);

        var resultado = await validator.ValidarAsync(
            new CategoriaInput
            {
                Nombre = "Categoría válida",
                Ubicacion = "INVALIDA"
            });

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("Ubicacion", resultado.Errores.Keys);
    }

    [Fact]
    public async Task Categoria_con_producto_activo_no_se_puede_dar_de_baja()
    {
        var categoria = new Categoria
        {
            PublicId = Guid.NewGuid(),
            Codigo = "CAT-001",
            Nombre = "Arte",
            Ubicacion = "Bodega",
            Estado = true
        };

        var repository = new CategoriaRepositoryFalso
        {
            TieneRelaciones = true
        };

        repository.Agregar(categoria);

        var factory =
            new CrudRepositoryFactoryFalsa<Categoria>(
                repository);

        var useCase =
            new CambiarEstadoCategoriaUseCase(factory);

        var resultado =
            await useCase.EjecutarAsync(
                categoria.PublicId,
                false);

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("Estado", resultado.Errores.Keys);
        Assert.True(categoria.Estado);
    }

    [Fact]
    public async Task Renombrar_categoria_actualiza_referencia_embebida()
    {
        var categoria = new Categoria
        {
            PublicId = Guid.NewGuid(),
            Codigo = "CAT-001",
            Nombre = "Arte",
            Ubicacion = "Bodega",
            Estado = true
        };

        var repository = new CategoriaRepositoryFalso();
        repository.Agregar(categoria);

        var productos = new ProductoRepositoryFalso();

        var producto = CrearProducto(
            categoriaPublicId: categoria.PublicId,
            categoriaNombre: "Arte",
            marcaPublicId: Guid.NewGuid(),
            marcaNombre: "Marca");

        productos.Agregar(producto);

        var factory =
            new CrudRepositoryFactoryFalsa<Categoria>(
                repository);

        var validator =
            new CategoriaValidator(repository);

        var useCase =
            new ActualizarCategoriaUseCase(
                factory,
                validator,
                productos);

        var resultado =
            await useCase.EjecutarAsync(
                categoria.PublicId,
                new CategoriaInput
                {
                    Codigo = categoria.Codigo,
                    Nombre = "Arte actualizado",
                    Descripcion = "Actualizada",
                    Ubicacion = "Bodega"
                });

        Assert.True(resultado.EsExito);

        var persistido =
            await productos.ObtenerPorPublicIdAsync(
                producto.PublicId,
                true);

        Assert.NotNull(persistido);
        Assert.Equal(
            "Arte actualizado",
            persistido.Categoria.Nombre);
    }

    [Fact]
    public async Task Marca_nombre_duplicado_devuelve_validacion()
    {
        var repository = new MarcaRepositoryFalso();

        repository.Agregar(new Marca
        {
            PublicId = Guid.NewGuid(),
            Nombre = "Marca prueba",
            PaisOrigen = "Bolivia",
            Estado = true
        });

        var validator = new MarcaValidator(repository);

        var resultado = await validator.ValidarAsync(
            new MarcaInput
            {
                Nombre = "Marca prueba",
                PaisOrigen = "Bolivia"
            });

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("Nombre", resultado.Errores.Keys);
    }

    [Fact]
    public async Task Marca_pais_invalido_devuelve_validacion()
    {
        var repository = new MarcaRepositoryFalso();
        var validator = new MarcaValidator(repository);

        var resultado = await validator.ValidarAsync(
            new MarcaInput
            {
                Nombre = "Marca válida",
                PaisOrigen = "PAIS_INVALIDO"
            });

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Validacion, resultado.Tipo);
        Assert.Contains("PaisOrigen", resultado.Errores.Keys);
    }

    [Fact]
    public async Task Marca_con_producto_activo_no_se_puede_dar_de_baja()
    {
        var marca = new Marca
        {
            PublicId = Guid.NewGuid(),
            Nombre = "Marca prueba",
            PaisOrigen = "Bolivia",
            Estado = true
        };

        var repository = new MarcaRepositoryFalso
        {
            TieneRelaciones = true
        };

        repository.Agregar(marca);

        var factory =
            new CrudRepositoryFactoryFalsa<Marca>(
                repository);

        var useCase =
            new CambiarEstadoMarcaUseCase(factory);

        var resultado =
            await useCase.EjecutarAsync(
                marca.PublicId,
                false);

        Assert.False(resultado.EsExito);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("Estado", resultado.Errores.Keys);
        Assert.True(marca.Estado);
    }

    [Fact]
    public async Task Renombrar_marca_actualiza_referencia_embebida()
    {
        var marca = new Marca
        {
            PublicId = Guid.NewGuid(),
            Nombre = "Marca prueba",
            PaisOrigen = "Bolivia",
            Estado = true
        };

        var repository = new MarcaRepositoryFalso();
        repository.Agregar(marca);

        var productos = new ProductoRepositoryFalso();

        var producto = CrearProducto(
            categoriaPublicId: Guid.NewGuid(),
            categoriaNombre: "Categoría",
            marcaPublicId: marca.PublicId,
            marcaNombre: "Marca prueba");

        productos.Agregar(producto);

        var factory =
            new CrudRepositoryFactoryFalsa<Marca>(
                repository);

        var validator =
            new MarcaValidator(repository);

        var useCase =
            new ActualizarMarcaUseCase(
                factory,
                validator,
                productos);

        var resultado =
            await useCase.EjecutarAsync(
                marca.PublicId,
                new MarcaInput
                {
                    Nombre = "Marca actualizada",
                    Descripcion = "Actualizada",
                    PaisOrigen = "Bolivia",
                    SitioWeb = "https://example.com"
                });

        Assert.True(resultado.EsExito);

        var persistido =
            await productos.ObtenerPorPublicIdAsync(
                producto.PublicId,
                true);

        Assert.NotNull(persistido);
        Assert.Equal(
            "Marca actualizada",
            persistido.Marca.Nombre);
    }

    private static Producto CrearProducto(
        Guid categoriaPublicId,
        string categoriaNombre,
        Guid marcaPublicId,
        string marcaNombre)
    {
        return new Producto
        {
            PublicId = Guid.NewGuid(),
            Nombre = "Producto prueba",
            Stock = 10,
            PrecioVenta = 15m,
            CostoAdquisicionActual = 10m,
            Estado = true,
            Categoria = new ReferenciaCatalogo
            {
                PublicId = categoriaPublicId,
                Nombre = categoriaNombre
            },
            Marca = new ReferenciaCatalogo
            {
                PublicId = marcaPublicId,
                Nombre = marcaNombre
            },
            FechaCreacion = DateTime.UtcNow
        };
    }

    private sealed class CrudRepositoryFactoryFalsa<TEntity>
        : CrudRepositoryFactory<TEntity>
        where TEntity : class
    {
        private readonly ICrudRepository<TEntity> _repository;

        public CrudRepositoryFactoryFalsa(
            ICrudRepository<TEntity> repository)
        {
            _repository = repository;
        }

        public override ICrudRepository<TEntity>
            CrearRepositorio()
        {
            return _repository;
        }
    }

    private sealed class CategoriaRepositoryFalso
        : ICrudRepository<Categoria>
    {
        private readonly List<Categoria> _categorias = [];

        public bool TieneRelaciones { get; set; }

        public void Agregar(Categoria categoria)
            => _categorias.Add(categoria);

        public Task<IReadOnlyList<Categoria>>
            ObtenerActivasAsync(string? busqueda = null)
        {
            IEnumerable<Categoria> consulta =
                _categorias.Where(item => item.Estado);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                consulta = consulta.Where(item =>
                    item.Nombre.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<Categoria>>(
                consulta.ToList());
        }

        public Task<Categoria?> ObtenerPorPublicIdAsync(
            Guid publicId,
            bool estadoEsperado)
        {
            return Task.FromResult(
                _categorias.FirstOrDefault(item =>
                    item.PublicId == publicId
                    && item.Estado == estadoEsperado));
        }

        public Task CrearAsync(Categoria entidad)
        {
            if (entidad.PublicId == Guid.Empty)
                entidad.PublicId = Guid.NewGuid();

            entidad.Codigo =
                string.IsNullOrWhiteSpace(entidad.Codigo)
                    ? "CAT-TEST"
                    : entidad.Codigo;

            entidad.Estado = true;

            _categorias.Add(entidad);

            return Task.CompletedTask;
        }

        public Task<bool> ActualizarAsync(
            Categoria entidad)
        {
            return Task.FromResult(
                _categorias.Any(item =>
                    item.PublicId == entidad.PublicId
                    && item.Estado));
        }

        public Task<bool> CambiarEstadoAsync(
            Guid publicId,
            bool nuevoEstado)
        {
            var categoria =
                _categorias.FirstOrDefault(item =>
                    item.PublicId == publicId
                    && item.Estado != nuevoEstado);

            if (categoria is null)
                return Task.FromResult(false);

            categoria.Estado = nuevoEstado;

            return Task.FromResult(true);
        }

        public Task<bool> ExisteValorAsync(
            string campo,
            string valor,
            Guid? excluirPublicId = null)
        {
            var existe = _categorias.Any(item =>
                (!excluirPublicId.HasValue
                    || item.PublicId != excluirPublicId.Value)
                && campo == "Nombre"
                && string.Equals(
                    item.Nombre,
                    valor,
                    StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(existe);
        }

        public Task<bool> TieneRelacionesAsync(
            Guid publicId)
        {
            return Task.FromResult(TieneRelaciones);
        }
    }

    private sealed class MarcaRepositoryFalso
        : ICrudRepository<Marca>
    {
        private readonly List<Marca> _marcas = [];

        public bool TieneRelaciones { get; set; }

        public void Agregar(Marca marca)
            => _marcas.Add(marca);

        public Task<IReadOnlyList<Marca>>
            ObtenerActivasAsync(string? busqueda = null)
        {
            IEnumerable<Marca> consulta =
                _marcas.Where(item => item.Estado);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                consulta = consulta.Where(item =>
                    item.Nombre.Contains(
                        busqueda,
                        StringComparison.OrdinalIgnoreCase));
            }

            return Task.FromResult<IReadOnlyList<Marca>>(
                consulta.ToList());
        }

        public Task<Marca?> ObtenerPorPublicIdAsync(
            Guid publicId,
            bool estadoEsperado)
        {
            return Task.FromResult(
                _marcas.FirstOrDefault(item =>
                    item.PublicId == publicId
                    && item.Estado == estadoEsperado));
        }

        public Task CrearAsync(Marca entidad)
        {
            if (entidad.PublicId == Guid.Empty)
                entidad.PublicId = Guid.NewGuid();

            entidad.Estado = true;

            _marcas.Add(entidad);

            return Task.CompletedTask;
        }

        public Task<bool> ActualizarAsync(
            Marca entidad)
        {
            return Task.FromResult(
                _marcas.Any(item =>
                    item.PublicId == entidad.PublicId
                    && item.Estado));
        }

        public Task<bool> CambiarEstadoAsync(
            Guid publicId,
            bool nuevoEstado)
        {
            var marca =
                _marcas.FirstOrDefault(item =>
                    item.PublicId == publicId
                    && item.Estado != nuevoEstado);

            if (marca is null)
                return Task.FromResult(false);

            marca.Estado = nuevoEstado;

            return Task.FromResult(true);
        }

        public Task<bool> ExisteValorAsync(
            string campo,
            string valor,
            Guid? excluirPublicId = null)
        {
            var existe = _marcas.Any(item =>
                (!excluirPublicId.HasValue
                    || item.PublicId != excluirPublicId.Value)
                && campo == "Nombre"
                && string.Equals(
                    item.Nombre,
                    valor,
                    StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(existe);
        }

        public Task<bool> TieneRelacionesAsync(
            Guid publicId)
        {
            return Task.FromResult(TieneRelaciones);
        }
    }
}