using Libreria.Servicio.Ventas.Application.Facades;
using Libreria.Servicio.Ventas.Application.Factories;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Services;
using Libreria.Servicio.Ventas.Application.Validators;
using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Infrastructure;
using Libreria.Servicio.Ventas.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// 1. API REST + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Cadena de conexión (Singleton) y fábrica de conexiones
var cadenaConexion = builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión: defina DB_CONNECTION_STRING o ConnectionStrings:LibreriaDb.");

var conexiones = ConnectionStringSingleton.Instancia;
conexiones.Configurar(cadenaConexion);

builder.Services.AddSingleton(conexiones);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

// 3. Usuario que opera: lo informa el Front por encabezado (puerto IUsuarioActual)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualDesdeEncabezados>();

// 4. Validadores
builder.Services.AddScoped<CategoriaValidator>();
builder.Services.AddScoped<MarcaValidator>();
builder.Services.AddScoped<ProductoValidator>();
builder.Services.AddScoped<ClienteValidator>();

// 5. Fábricas base del Factory Method y puertos secundarios
builder.Services.AddScoped<CrudRepositoryFactory<Categoria>, CategoriaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Marca>, MarcaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Producto>, ProductoRepositoryFactory>();
builder.Services.AddScoped<HistoricoRepositoryFactory, HistoricoCostoRepositoryFactory>();

builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddScoped<IHistoricoCostoRepository, HistoricoCostoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IMovimientoStockRepository, MovimientoStockRepository>();

// 6. Servicios y fachadas (puertos primarios)
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IStockFacade, StockFacade>();
builder.Services.AddScoped<IVentaFacade, VentaFacade>();
builder.Services.AddScoped<IConsultaVentaService, ConsultaVentaService>();
builder.Services.AddScoped<IComprobanteVentaService, ComprobanteVentaService>();
builder.Services.AddScoped<IClienteService, ClienteService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();

app.Run();
