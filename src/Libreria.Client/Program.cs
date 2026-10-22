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
using Libreria.Infrastructure.Email;
using Libreria.Infrastructure.Configuration;
using Libreria.Application.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");

        options.Conventions.AuthorizeFolder("/Categorias", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Marcas", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Productos", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Historico", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Diagnostico", PoliticasAcceso.SoloAdministrador);

        options.Conventions.AuthorizeFolder("/Ventas", PoliticasAcceso.Ventas);
    })
    .AddMvcOptions(options =>
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

// Llamadas a las extensiones modulares
builder.Services.AddRedisCaching(builder.Configuration);
builder.Services
    .AddOptions<ProductosActivosCacheOptions>()
    .Bind(builder.Configuration.GetSection(ProductosActivosCacheOptions.SectionName))
    .Validate(
        options => options.ExpiracionMinutos > 0,
        "La expiración de la caché de productos debe ser mayor a cero.")
    .ValidateOnStart();
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
builder.Services.AddScoped<PasswordPolicyValidator>();

// Servicios y Repositorios adicionales
builder.Services.AddScoped<ICatalogoProductoRepository, CatalogoProductoRepository>();
builder.Services.AddSingleton<CacheDiagnosticoService>();
builder.Services.AddScoped<ProductosActivosService>();
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

// Seguridad y Auditoría
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"/keys"))
    .SetApplicationName("LibreriaCompartida");

builder.Services.AddScoped<Libreria.Application.Ports.Primary.IUrlProtector, Libreria.Infrastructure.Security.UrlProtector>();
builder.Services.AddScoped<IContrasenaHasher, Libreria.Infrastructure.Security.ContrasenaHasher>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, Libreria.Infrastructure.Security.UsuarioActual>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

// US-38: Autenticación por cookie de ASP.NET Core
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
        options.EventsType = typeof(ValidarEstadoUsuarioCookieEvents);
    });

builder.Services.AddSingleton<ITicketStore, RedisTicketStore>();
builder.Services
    .AddOptions<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, store) =>
    {
        options.SessionStore = store;
    });

// US-39: Políticas de autorización por rol
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PoliticasAcceso.SoloAdministrador, politica =>
        politica.RequireRole(RolesSistema.Administrador));

    options.AddPolicy(PoliticasAcceso.Ventas, politica =>
        politica.RequireRole(RolesSistema.Administrador, RolesSistema.Vendedor));
});

builder.Services.AddScoped<ValidarEstadoUsuarioCookieEvents>();

var connectionString = builder.Configuration["DB_CONNECTION_STRING"] 
    ?? builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException("Falta la cadena de conexión");

var connectionStringSingleton = ConnectionStringSingleton.Instancia;
connectionStringSingleton.Configurar(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();

var app = builder.Build();

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
    public static IServiceCollection AddUsuariosModule(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IGeneradorNombreUsuario, GeneradorNombreUsuario>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IConfiguracionAplicacion, ConfiguracionAplicacion>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IConsultaUsuarioService, ConsultaUsuarioService>();
        services.AddScoped<IUsuarioSeguridadRepository, UsuarioRepository>();
        services.AddScoped<IUsuarioSeguridadService, UsuarioSeguridadService>();
        services.AddScoped<IRolRepository, RolRepository>();

        return services;
    }

}
