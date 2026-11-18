using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Infrastructure.Seguridad;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using MongoDB.Driver;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Factories;
using Libreria.Servicio.Productos.Infrastructure.Repositories;
using Libreria.Servicio.Productos.Application.UseCases.Productos;
using Libreria.Servicio.Productos.Application.Validators;
using Libreria.Servicio.Productos.Application.UseCases.Categorias;
using Libreria.Servicio.Productos.Application.UseCases.Marcas;
using Libreria.Servicio.Productos.Application.UseCases.Historico;
using Libreria.Servicio.Productos.Presentation.Comun;

var builder = WebApplication.CreateBuilder(args);

MongoSerialization.Configurar();

var productosDb = builder.Configuration.GetConnectionString("ProductosDb")
    ?? throw new InvalidOperationException(
        "Falta la configuración ConnectionStrings:ProductosDb.");

var mongoUrl = new MongoUrl(productosDb);

if (string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
{
    throw new InvalidOperationException(
        "ConnectionStrings:ProductosDb debe incluir el nombre de la base de datos.");
}

builder.Services.AddSingleton<IMongoClient>(
    _ => new MongoClient(mongoUrl));

builder.Services.AddSingleton<IMongoDatabase>(sp =>
    sp.GetRequiredService<IMongoClient>()
      .GetDatabase(mongoUrl.DatabaseName));

builder.Services.AddSingleton<MongoContext>();

// Repositorios Mongo: adaptadores de salida de Clean Architecture.
builder.Services.AddScoped<IProductoRepository, MongoProductoRepository>();

builder.Services.AddScoped<
    ICatalogoProductoRepository,
    MongoCatalogoProductoRepository>();

builder.Services.AddScoped<
    IHistoricoCostoRepository,
    MongoHistoricoCostoRepository>();

// Factory Method: Concrete Creators Mongo.
builder.Services.AddScoped<
    CrudRepositoryFactory<Producto>,
    MongoProductoRepositoryFactory>();

builder.Services.AddScoped<
    CrudRepositoryFactory<Categoria>,
    MongoCategoriaRepositoryFactory>();

builder.Services.AddScoped<
    CrudRepositoryFactory<Marca>,
    MongoMarcaRepositoryFactory>();

builder.Services.AddScoped<
    HistoricoRepositoryFactory,
    MongoHistoricoRepositoryFactory>();


builder.Services.AddScoped<ObtenerListadoProductosUseCase>();
builder.Services.AddScoped<ObtenerDetalleProductoUseCase>();

builder.Services.AddScoped<
    ICrudRepository<Categoria>,
    MongoCategoriaRepository>();

builder.Services.AddScoped<
    ICrudRepository<Marca>,
    MongoMarcaRepository>();
builder.Services.AddScoped<CategoriaValidator>();
builder.Services.AddScoped<MarcaValidator>();

builder.Services.AddScoped<ListarCategoriasUseCase>();
builder.Services.AddScoped<ObtenerCategoriaUseCase>();
builder.Services.AddScoped<CrearCategoriaUseCase>();
builder.Services.AddScoped<ActualizarCategoriaUseCase>();
builder.Services.AddScoped<CambiarEstadoCategoriaUseCase>();
builder.Services.AddScoped<ListarMarcasUseCase>();
builder.Services.AddScoped<ObtenerMarcaUseCase>();
builder.Services.AddScoped<CrearMarcaUseCase>();
builder.Services.AddScoped<ActualizarMarcaUseCase>();
builder.Services.AddScoped<CambiarEstadoMarcaUseCase>();
builder.Services.AddScoped<ProductoValidator>();
builder.Services.AddScoped<ObtenerFormularioProductoUseCase>();
builder.Services.AddScoped<CrearProductoUseCase>();
builder.Services.AddScoped<ObtenerProductoEdicionUseCase>();
builder.Services.AddScoped<ActualizarProductoUseCase>();
builder.Services.AddScoped<CambiarEstadoProductoUseCase>();
builder.Services.AddScoped<ObtenerHistoricoProductoUseCase>();
// Errores que no son de negocio → ProblemDetails: BD caída 503, inesperado 500 (US-139, contrato 6.5).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

// 1. API REST + Swagger
builder.Services.AddControllers(opciones =>
    opciones.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Usuario que opera (puerto IUsuarioActual). Hasta US-127 solo existe el de desarrollo.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IUsuarioActual, UsuarioActualDesarrollo>();
}

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.MapGet("/salud", () => Results.Ok(new { servicio = "Productos", estado = "ok" }));

app.Run();
