using Libreria.Application.Factories;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Services;
using Libreria.Application.Validators;
using Libreria.Client.Clients;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.DependencyInjection;
using Libreria.Client.Seguridad;
using Libreria.Client.Services.Auditoria;
using Libreria.Domain;
using Libreria.Infrastructure;
using Libreria.Infrastructure.Configuration;
using Libreria.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");

        options.Conventions.AuthorizeFolder("/Categorias", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Marcas", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Productos", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Historico", PoliticasAcceso.SoloAdministrador);
        options.Conventions.AuthorizeFolder("/Reportes", PoliticasAcceso.SoloAdministrador);

        options.Conventions.AuthorizeFolder("/Ventas", PoliticasAcceso.Ventas);
    })
    .AddMvcOptions(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        options.Filters.Add<ServicioNoDisponibleFilter>();
    });

builder.Services
    .AddContextoUsuarioServicios()
    .AddServicioVentas(builder.Configuration)
    .AddVentasApiClient(builder.Configuration)
    .AddServicioUsuarios(builder.Configuration)
    .AddServicioReportes(builder.Configuration);

builder.Services.AddMemoryCache();
builder.Services.Configure<SesionActivaOptions>(
    builder.Configuration.GetSection(SesionActivaOptions.SectionName));


// ---------------------------------------------------------------------------
// TEMPORAL: módulo de Usuarios y Cuenta.
// Sigue funcionando con las bibliotecas antiguas hasta que Grupo 2 lo migre a
// Libreria.Servicio.Usuarios (US-92/US-93). Entonces se elimina todo este bloque.
// ---------------------------------------------------------------------------
builder.Services.AddUsuariosModule();

builder.Services.AddScoped<UsuarioRepositoryFactory, SqlUsuarioRepositoryFactory>();
builder.Services.AddScoped<LoginValidator>();
builder.Services.AddScoped<PasswordPolicyValidator>();
builder.Services.AddScoped<IAutenticacionService, AutenticacionService>();

// Seguridad y Auditoría
builder.Services.AddDataProtection()
    .SetApplicationName("Libreria.Client");

builder.Services.AddScoped<IUrlProtector, UrlProtector>();
builder.Services.AddScoped<IContrasenaHasher, Libreria.Infrastructure.Security.ContrasenaHasher>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, Libreria.Infrastructure.Security.UsuarioActual>();
builder.Services.AddScoped<
    Libreria.Client.Services.Auditoria.IAuditoriaService,
    Libreria.Client.Services.Auditoria.AuditoriaService>();

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.EventsType = typeof(ValidarEstadoUsuarioCookieEvents);
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

// Cadena de conexión que usa el módulo de Usuarios mientras viva en el Front.
var connectionString = builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException("Falta la cadena de conexión");

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
