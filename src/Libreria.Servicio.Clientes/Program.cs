using Libreria.Servicio.Clientes.Application.Factories;
using Libreria.Servicio.Clientes.Application.Ports.Primary;
using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Application.Services;
using Libreria.Servicio.Clientes.Application.Validators;
using Libreria.Servicio.Clientes.Controllers.Comun;
using Libreria.Servicio.Clientes.Infrastructure.Connection;
using Libreria.Servicio.Clientes.Infrastructure.Factories;
using Libreria.Servicio.Clientes.Infrastructure.Seguridad;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// 1. API REST + Swagger
// Sin [Required] implícito por ser string no anulable: así un campo nulo (el Front envía null
// si el usuario lo deja vacío) llega a ClienteValidator y vuelve como Result con mensajes en español.
builder.Services.AddControllers(opciones =>
    opciones.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones => opciones.AgregarBearerDeKeycloak());

// US-126: todo exige el JWT de Keycloak (firma RS256, emisor, audiencia libreria-api, vencimiento)
// y los roles se aplican con [Authorize(Policy = ...)]. Ver Infrastructure/Seguridad/SeguridadJwtExtensions.cs.
builder.Services.AddSeguridadJwt(builder.Configuration, builder.Environment);

// Errores que no son de negocio → ProblemDetails: BD caída 503, inesperado 500 (contrato 6.5).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

// 2. Cadena de conexión (Singleton) y fábrica de conexiones MySQL (contrato 6.1:
//    appsettings sin contraseña; la real llega por ConnectionStrings__ClientesDb).
var cadenaClientes = builder.Configuration.GetConnectionString("ClientesDb");
if (string.IsNullOrWhiteSpace(cadenaClientes))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión: defina ConnectionStrings__ClientesDb o ConnectionStrings:ClientesDb.");
}

var conexiones = ConnectionStringSingleton.Instancia;
conexiones.Configurar(cadenaClientes);
builder.Services.AddSingleton(conexiones);
builder.Services.AddScoped<IDbConnectionFactory, MySqlConnectionFactory>();

// 3. Usuario que opera (puerto IUsuarioActual). Hasta US-127 solo existe el de desarrollo.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IUsuarioActual, UsuarioActualDesarrollo>();
}

// 4. Factory Method: el servicio pide su repositorio a la fábrica, no al contenedor.
builder.Services.AddScoped<ClienteRepositoryFactory, MySqlClienteRepositoryFactory>();

// 5. Núcleo: validador y casos de uso (puerto primario)
builder.Services.AddScoped<ClienteValidator>();
builder.Services.AddScoped<IClienteService, ClienteService>();

// Detrás del gateway: YARP envía X-Forwarded-*; así el Location del 201 apunta al gateway (5080), no a 5103.
// Por defecto solo se confía en proxies de loopback (el gateway en la misma máquina).
builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
    opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/salud", () => Results.Ok(new { servicio = "Clientes", estado = "ok" }))
    .AllowAnonymous();

app.Run();

// Visible para las pruebas de la API (WebApplicationFactory).
public partial class Program;
