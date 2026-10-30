using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.UseCases.Auth;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Infrastructure.Connection;
using Libreria.Servicio.Usuarios.Infrastructure.Repositories;
using Libreria.Servicio.Usuarios.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var connectionString =
    builder.Configuration["DB_CONNECTION_STRING"]
    ?? builder.Configuration.GetConnectionString("LibreriaDb")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión de LibreriaDb.");

var connectionStringSingleton =
    ConnectionStringSingleton.Instance;

connectionStringSingleton.Configure(connectionString);

builder.Services.AddSingleton(connectionStringSingleton);
builder.Services.AddScoped<SqlConnectionFactory>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();

builder.Services.AddScoped<LoginValidator>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<CheckActiveSessionUseCase>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();