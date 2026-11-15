using Libreria.Servicio.Ventas.Application.Facades;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Services;
using Libreria.Servicio.Ventas.Infrastructure;
using Libreria.Servicio.Ventas.Infrastructure.Adapters;
using Libreria.Servicio.Ventas.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IUsuarioActual, UsuarioActualDesarrollo>();
    builder.Services.AddSingleton<IClientesPort, ClientesDesarrolloAdapter>();
    builder.Services.AddSingleton<IProductosPort, ProductosDesarrolloAdapter>();
}

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

public partial class Program;
