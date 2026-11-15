using Libreria.Servicio.Ventas.Application.Facades;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Services;
using Libreria.Servicio.Ventas.Infrastructure;
using Libreria.Servicio.Ventas.Infrastructure.Adapters;
using Libreria.Servicio.Ventas.Infrastructure.Http;
using Libreria.Servicio.Ventas.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();

var cadenaVentas = builder.Configuration.GetConnectionString("VentasDb");
if (string.IsNullOrWhiteSpace(cadenaVentas))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión: defina ConnectionStrings__VentasDb o ConnectionStrings:VentasDb.");
}

var conexiones = ConnectionStringSingleton.Instancia;
conexiones.Configurar(cadenaVentas);

builder.Services.AddSingleton(conexiones);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

builder.Services.AddScoped<IUsuarioActual, UsuarioActualHttpContext>();

builder.Services.AddTransient<PropagarAutenticacionHandler>();
builder.Services
    .AddHttpClient<IClientesPort, ClientesHttpAdapter>(client =>
    {
        client.BaseAddress = ObtenerUrlServicio(builder.Configuration, "Clientes");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddHttpMessageHandler<PropagarAutenticacionHandler>();
builder.Services
    .AddHttpClient<IProductosPort, ProductosHttpAdapter>(client =>
    {
        client.BaseAddress = ObtenerUrlServicio(builder.Configuration, "Productos");
        client.Timeout = TimeSpan.FromSeconds(5);
    })
    .AddHttpMessageHandler<PropagarAutenticacionHandler>();

builder.Services.AddScoped<IVentaRepository, VentaRepository>();
builder.Services.AddScoped<IMovimientoStockRepository, MovimientoStockRepository>();
builder.Services.AddScoped<IStockFacade, StockFacade>();
builder.Services.AddScoped<IVentaFacade, VentaFacade>();
builder.Services.AddScoped<IComprobanteVentaService, ComprobanteVentaService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.MapGet("/salud", () =>
    Results.Ok(new { servicio = "Ventas", estado = "ok" }));

app.Run();

static Uri ObtenerUrlServicio(IConfiguration configuracion, string servicio)
{
    var valor = configuracion[$"Servicios:{servicio}:BaseUrl"];
    if (!Uri.TryCreate(valor, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException(
            $"Falta Servicios:{servicio}:BaseUrl o no contiene una URL absoluta.");
    }

    return uri;
}

public partial class Program;
