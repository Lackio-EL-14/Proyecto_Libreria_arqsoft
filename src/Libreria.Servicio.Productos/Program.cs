using Libreria.Servicio.Productos.Application.Abstractions;
using Libreria.Servicio.Productos.Infrastructure.Seguridad;
using Libreria.Servicio.Productos.Infrastructure.Mongo;
using MongoDB.Driver;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Domain.Entities;
using Libreria.Servicio.Productos.Infrastructure.Factories;
using Libreria.Servicio.Productos.Infrastructure.Repositories;
using Libreria.Servicio.Productos.Application.UseCases.Productos;

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


// Casos de uso de Application.
builder.Services.AddScoped<ObtenerListadoProductosUseCase>();
builder.Services.AddScoped<ObtenerDetalleProductoUseCase>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.MapGet("/salud", () => Results.Ok(new { servicio = "Productos", estado = "ok" }));

app.Run();
