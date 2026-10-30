using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Libreria.Servicio.Ventas.Application.Validators;
using Libreria.Servicio.Ventas.Application.Factories;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Services;
using Libreria.Servicio.Ventas.Application.Facades;
using Libreria.Servicio.Ventas.Infrastructure;
using Libreria.Servicio.Ventas.Infrastructure.Security;
using Libreria.Servicio.Ventas.Domain;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Swagger y CORS (US-83)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Configurar Controladores REST
builder.Services.AddControllers();

// 3. Configurar la Cadena de Conexión y Factory de SQL
var connectionString = builder.Configuration["DB_CONNECTION_STRING"] 
    ?? builder.Configuration.GetConnectionString("LibreriaDb")
    ?? "Server=localhost;Database=LibreriaDb;Integrated Security=True;TrustServerCertificate=True;";

var connectionStringSingleton = ConnectionStringSingleton.Instancia;
connectionStringSingleton.Configurar(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

// 4. Registrar IHttpContextAccessor
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();

// 5. Inyección de Validadores (Hito A y B)
builder.Services.AddScoped<CategoriaValidator>();
builder.Services.AddScoped<MarcaValidator>();
builder.Services.AddScoped<ProductoValidator>();
builder.Services.AddScoped<ClienteValidator>();

// 6. Inyección de Factories y Puertos Secundarios (Infraestructura)
builder.Services.AddScoped<CrudRepositoryFactory<Categoria>, CategoriaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Marca>, MarcaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Producto>, ProductoRepositoryFactory>();
builder.Services.AddScoped<HistoricoRepositoryFactory, HistoricoCostoRepositoryFactory>();

builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddScoped<IHistoricoCostoRepository, HistoricoCostoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();

// 7. Inyección de Fachadas y Puertos Primarios (Aplicación)
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IStockFacade, StockFacade>();
builder.Services.AddScoped<IVentaFacade, VentaFacade>();
builder.Services.AddScoped<IConsultaVentaService, ConsultaVentaService>();
builder.Services.AddScoped<IComprobanteVentaService, ComprobanteVentaService>();
builder.Services.AddScoped<IClienteService, ClienteService>();

// Configuración del cliente para la API de Ventas (US-83)
builder.Services.AddHttpClient("VentasAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5101/");
});

var app = builder.Build();

// 8. Configurar el Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// CORS debe ir entre UseRouting y UseAuthorization
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.Run();
