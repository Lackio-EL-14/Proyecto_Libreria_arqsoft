using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Factories;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.UseCases.Auth;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Infrastructure.Configuration;
using Libreria.Servicio.Usuarios.Infrastructure.Connection;
using Libreria.Servicio.Usuarios.Infrastructure.Email;
using Libreria.Servicio.Usuarios.Infrastructure.Factories;
using Libreria.Servicio.Usuarios.Infrastructure.Repositories;
using Libreria.Servicio.Usuarios.Presentation.Security;
using Libreria.Servicio.Usuarios.Presentation.Middleware;
using Libreria.Servicio.Usuarios.Infrastructure.Security;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

var connectionString =
    builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión de LibreriaDb.");

var connectionStringSingleton =
    ConnectionStringSingleton.Instance;

connectionStringSingleton.Configure(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<SqlConnectionFactory>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

builder.Services.AddScoped<
    UsuarioRepositoryFactory,
    SqlUsuarioRepositoryFactory>();

builder.Services.AddScoped<LoginValidator>();

builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<CheckActiveSessionUseCase>();
builder.Services.AddScoped<GetUserNamesUseCase>();

builder.Services.AddScoped<UsernameGenerator>();
builder.Services.AddScoped<UsuarioFacade>();

builder.Services.AddScoped<
    IApplicationConfiguration,
    ApplicationConfiguration>();

builder.Services.AddScoped<
    IEmailSender,
    SmtpEmailSender>();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseMiddleware<CurrentUserMiddleware>();

app.MapControllers();

app.Run();