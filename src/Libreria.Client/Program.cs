using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Infrastructure;
using Libreria.Application.Validators;
using Libreria.Application.Services;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Facades;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages(options =>
        // US-38: equivale a [Authorize] en todas las páginas; solo el login
        // (y la página de error) quedan públicas con [AllowAnonymous].
        options.Conventions.AuthorizeFolder("/"))
    .AddMvcOptions(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

// Fábricas base del Factory Method
builder.Services.AddScoped<CrudRepositoryFactory<Categoria>, CategoriaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Marca>, MarcaRepositoryFactory>();
builder.Services.AddScoped<CrudRepositoryFactory<Producto>, ProductoRepositoryFactory>();
builder.Services.AddScoped<HistoricoRepositoryFactory, HistoricoCostoRepositoryFactory>();
builder.Services.AddScoped<UsuarioRepositoryFactory, SqlUsuarioRepositoryFactory>();

// Validadores
builder.Services.AddScoped<CategoriaValidator>();
builder.Services.AddScoped<MarcaValidator>();
builder.Services.AddScoped<ProductoValidator>();
builder.Services.AddScoped<ClienteValidator>();
builder.Services.AddScoped<LoginValidator>();

// Servicios y Repositorios adicionales (Catálogo, Histórico)
builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();
builder.Services.AddScoped<IHistoricoCostoRepository, HistoricoCostoRepository>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IVentaRepository, VentaRepository>();

builder.Services.AddScoped<IStockFacade, StockFacade>();
builder.Services.AddScoped<IVentaFacade, VentaFacade>();
builder.Services.AddDataProtection();
builder.Services.AddScoped<Libreria.Application.Ports.Primary.IUrlProtector, Libreria.Infrastructure.Security.UrlProtector>();
builder.Services.AddScoped<IContrasenaHasher, Libreria.Infrastructure.Security.ContrasenaHasher>();

// US-38: autenticación por cookie de ASP.NET Core
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.LogoutPath = "/Cuenta/Logout";
        options.Cookie.Name = "Libreria.Sesion";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var connectionString = builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'LibreriaDb' en appsettings.json");
var connectionStringSingleton = ConnectionStringSingleton.Instancia;
connectionStringSingleton.Configurar(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
