using Libreria.Client.Clients.Common;
using Libreria.Client.Configuracion;
using Libreria.Client.Adapters.DependencyInjection;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Seguridad;
using Libreria.Client.Services.Auditoria;
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

        options.Conventions.AuthorizeFolder("/Clientes", PoliticasAcceso.GestionClientes);
        options.Conventions.AuthorizeFolder("/Proveedores", PoliticasAcceso.SoloAdministrador);
    })
    .AddMvcOptions(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        // Pruebas del 22/11 (C3): ningún mensaje de enlace o validación en inglés en todo el sitio.
        options.UsarMensajesEnEspanol();
        options.Filters.Add<ServicioNoDisponibleFilter>();
        options.Filters.Add<SesionExpiradaFilter>();
    });

// Patrón Adapter (US-140): un HttpClient «ApiGateway» (Servicios:Gateway:BaseUrl) y un
// adaptador por API detrás de su interfaz (IClientesApi → ClientesApiAdapter, etc.).
builder.Services
    .AddContextoUsuarioServicios()
    .AddApiGateway(builder.Configuration)
    .AddModuloClientes()
    .AddModuloProveedores()
    .AddModuloProductos()
    .AddModuloUsuarios();

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

// US-125: login y sesiones con OpenID Connect contra Keycloak, y políticas por rol (US-39).
builder.Services.AddAutenticacionKeycloak(builder.Configuration, builder.Environment);

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

// Visible para las pruebas del Front (WebApplicationFactory).
public partial class Program;
