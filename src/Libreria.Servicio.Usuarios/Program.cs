using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.Factories;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Infrastructure.Email;
using Libreria.Servicio.Usuarios.Infrastructure.Configuration;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Infrastructure.Connection;
using Libreria.Servicio.Usuarios.Infrastructure.Factories;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Libreria.Servicio.Usuarios.Infrastructure.Seguridad;
using Libreria.Servicio.Usuarios.Infrastructure.Transactions;
using Libreria.Servicio.Usuarios.Presentation.Comun;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Errores que no son de negocio → ProblemDetails: BD o Keycloak caídos 503, inesperado 500 (contrato 6.5).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();

// Respuestas automáticas de ASP.NET Core en español (antes «One or more validation errors occurred.» y «Forbidden»).
builder.Services.Configure<ApiBehaviorOptions>(opciones =>
{
    opciones.ClientErrorMapping[StatusCodes.Status403Forbidden].Title = "No tiene permiso para realizar esta acción.";
    opciones.ClientErrorMapping[StatusCodes.Status404NotFound].Title = "No se encontró el recurso.";
    opciones.InvalidModelStateResponseFactory = contexto =>
        new BadRequestObjectResult(new ValidationProblemDetails(contexto.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Uno o más datos no son válidos."
        });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones => opciones.AgregarBearerDeKeycloak());

// US-126: todo exige el JWT de Keycloak (firma RS256, emisor, audiencia libreria-api, vencimiento)
// y los roles se aplican con [Authorize(Policy = ...)]. Ver Infrastructure/Seguridad/SeguridadJwtExtensions.cs.
builder.Services.AddSeguridadJwt(builder.Configuration, builder.Environment);
builder.Services.AddScoped<
    ITransactionRunner,
    TransactionRunner>();
builder.Services.AddHttpContextAccessor();
// US-126: el usuario actual sale del JWT validado (sub y preferred_username), no de cabeceras.
builder.Services.AddScoped<ICurrentUser, UsuarioActualDesdeToken>();

var connectionString =
    builder.Configuration.GetConnectionString("UsuariosDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión de UsuariosDb.");

var connectionStringSingleton =
    ConnectionStringSingleton.Instance;

connectionStringSingleton.Configure(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<SqlConnectionFactory>();

builder.Services.AddScoped<
    UsuarioRepositoryFactory,
    SqlUsuarioRepositoryFactory>();
builder.Services.AddScoped<IUserRepository>(provider =>
    provider.GetRequiredService<UsuarioRepositoryFactory>().CreateRepository());
builder.Services.AddScoped<ICheckActiveSessionUseCase, CheckActiveSessionUseCase>();
builder.Services.AddScoped<IGetUserNamesUseCase, GetUserNamesUseCase>();
builder.Services.AddScoped<IListUsersUseCase, ListUsersUseCase>();
builder.Services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
builder.Services.AddScoped<IGetUserRolesUseCase, GetUserRolesUseCase>();
builder.Services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
builder.Services.AddScoped<PasswordPolicyValidator>();
builder.Services.AddScoped<IDeactivateUserUseCase, DeactivateUserUseCase>();
builder.Services.AddScoped<UsernameGenerator>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IApplicationConfiguration, ApplicationConfiguration>();
builder.Services.AddScoped<UsuarioFacade>();
builder.Services.AddSingleton(TimeProvider.System);

// US-124: Keycloak como proveedor de identidad (puerto IProveedorIdentidad).
builder.Services.AddProveedorIdentidad(builder.Configuration, builder.Environment);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/salud", () => Results.Ok(new { servicio = "Usuarios", estado = "ok" }))
    .AllowAnonymous();

app.Run();

// Visible para las pruebas de la API (WebApplicationFactory).
public partial class Program;
