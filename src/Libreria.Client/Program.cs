using Libreria.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Infrastructure;
using Libreria.Application.Validators;
using Libreria.Application.Services;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Facades;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages(options =>
    {
        // (y la página de error) quedan públicas con [AllowAnonymous].
        options.Conventions.AuthorizeFolder("/");

        // US-39: el acceso por rol se valida en el servidor, no solo
        // ocultando el menú. Inventario e histórico: solo Administrador.
        options.Conventions.AuthorizeFolder("/Categorias", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Marcas", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Productos", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Historico", PoliticasAcceso.SoloAdministrador);

        // Ventas: Administrador y Vendedor (cubre las páginas que se agreguen en /Ventas).
        options.Conventions.AuthorizeFolder("/Ventas", PoliticasAcceso.Ventas);
    })
    .AddMvcOptions(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);


builder.Services.AddRedisCaching();
builder.Services.AddUsuariosModule();

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
builder.Services.AddScoped<IConsultaVentaService, ConsultaVentaService>();
builder.Services.AddScoped<IComprobanteVentaService, ComprobanteVentaService>();
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddDataProtection();
builder.Services.AddScoped<Libreria.Application.Ports.Primary.IUrlProtector, Libreria.Infrastructure.Security.UrlProtector>();
builder.Services.AddScoped<IContrasenaHasher, Libreria.Infrastructure.Security.ContrasenaHasher>();

// US-40: auditoría (usuario de la sesión actual + nombres para mostrar)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, Libreria.Infrastructure.Security.UsuarioActual>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

// US-38: autenticación por cookie de ASP.NET Core
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.LogoutPath = "/Cuenta/Logout";
        options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        options.Cookie.Name = "Libreria.Sesion";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// US-39: políticas de autorización por rol
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PoliticasAcceso.SoloAdministrador, politica =>
        politica.RequireRole(RolesSistema.Administrador));

    options.AddPolicy(PoliticasAcceso.Ventas, politica =>
        politica.RequireRole(RolesSistema.Administrador, RolesSistema.Vendedor));
});

var connectionString = builder.Configuration["DB_CONNECTION_STRING"] 
    ?? builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException("Falta la cadena de conexión");

var connectionStringSingleton = ConnectionStringSingleton.Instancia;
connectionStringSingleton.Configurar(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();


builder.Services.AddDataProtection()
.PersistKeysToFileSystem(new DirectoryInfo(@"/keys"))
.SetApplicationName("LibreriaCompartida");


var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});


app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});


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

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRedisCaching(this IServiceCollection services) => services;

    public static IServiceCollection AddUsuariosModule(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IGeneradorNombreUsuario, GeneradorNombreUsuario>();

        return services;
    }
}