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
using Libreria.Servicio.Ventas.Infrastructure;
using Libreria.Servicio.Ventas.Infrastructure;
using Libreria.Servicio.Ventas.Infrastructure.Security;
using Libreria.Servicio.Ventas.Domain;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar Controladores REST
builder.Services.AddControllers();

// 2. Configurar la Cadena de Conexión y Factory de SQL
var connectionString = builder.Configuration["DB_CONNECTION_STRING"] 
    ?? builder.Configuration.GetConnectionString("LibreriaDb")
    ?? "Server=localhost;Database=LibreriaDb;Integrated Security=True;TrustServerCertificate=True;";

var connectionStringSingleton = ConnectionStringSingleton.Instancia;
connectionStringSingleton.Configurar(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

// 3. Registrar IHttpContextAccessor para poder recuperar al usuario autenticado
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActual>();

// 4. Inyección de Validadores (Hito A y B)
builder.Services.AddScoped<CategoriaValidator>();
builder.Services.AddScoped<MarcaValidator>();
builder.Services.AddScoped<ProductoValidator>();
builder.Services.AddScoped<ClienteValidator>();

// 5. Inyección de Factories y Puertos Secundarios (Infraestructura)
builder.Services.AddScoped<CrudRepositoryFactory<Categoria>, CategoriaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Marca>, MarcaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Producto>, ProductoRepositoryFactory>();
builder.Services.AddScoped<HistoricoRepositoryFactory, HistoricoCostoRepositoryFactory>();

builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddScoped<IHistoricoCostoRepository, HistoricoCostoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();

// 6. Inyección de Fachadas y Puertos Primarios (Aplicación)
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IStockFacade, StockFacade>();
builder.Services.AddScoped<IVentaFacade, VentaFacade>();
builder.Services.AddScoped<IConsultaVentaService, ConsultaVentaService>();
builder.Services.AddScoped<IComprobanteVentaService, ComprobanteVentaService>();
builder.Services.AddScoped<IClienteService, ClienteService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
