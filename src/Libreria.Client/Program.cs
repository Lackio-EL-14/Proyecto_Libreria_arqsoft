using Libreria.Client.Clients;
using Libreria.Client.Clients.Common;
using Libreria.Client.Clients.DependencyInjection;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Seguridad;
using Libreria.Client.Services.Auditoria;
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
    .AddServicioUsuarios(builder.Configuration);

builder.Services.AddMemoryCache();
builder.Services.Configure<SesionActivaOptions>(
    builder.Configuration.GetSection(SesionActivaOptions.SectionName));

// Seguridad y Auditoría
builder.Services.AddDataProtection()
    .SetApplicationName("Libreria.Client");

builder.Services.AddScoped<IUrlProtector, UrlProtector>();

builder.Services.AddHttpContextAccessor();
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
