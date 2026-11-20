using Libreria.Servicio.Proveedores.Application.Factories;
using Libreria.Servicio.Proveedores.Application.Ports.Primary;
using Libreria.Servicio.Proveedores.Application.Ports.Secondary;
using Libreria.Servicio.Proveedores.Application.Services;
using Libreria.Servicio.Proveedores.Application.Validators;
using Libreria.Servicio.Proveedores.Controllers.Comun;
using Libreria.Servicio.Proveedores.Infrastructure.Connection;
using Libreria.Servicio.Proveedores.Infrastructure.Factories;
using Libreria.Servicio.Proveedores.Infrastructure.Seguridad;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AgregarBearerDeKeycloak());

// US-126: todo exige el JWT de Keycloak (firma RS256, emisor, audiencia libreria-api, vencimiento)
// y los roles se aplican con [Authorize(Policy = ...)]. Ver Infrastructure/Seguridad/SeguridadJwtExtensions.cs.
builder.Services.AddSeguridadJwt(builder.Configuration, builder.Environment);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

var cadena = builder.Configuration.GetConnectionString("ProveedoresDb");
if (string.IsNullOrWhiteSpace(cadena))
{
    throw new InvalidOperationException("Defina ConnectionStrings__ProveedoresDb.");
}

var singleton = ConnectionStringSingleton.Instancia;
singleton.Configurar(cadena);

builder.Services.AddSingleton(singleton);
builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioActual, UsuarioActualDesdeToken>();

builder.Services.AddScoped<ProveedorRepositoryFactory, SqlProveedorRepositoryFactory>();
builder.Services.AddScoped<ProveedorValidator>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();

var app = builder.Build();

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
app.MapGet(
    "/salud",
    () => Results.Ok(new { servicio = "Proveedores", estado = "ok" }))
    .AllowAnonymous();
app.Run();

public partial class Program;
