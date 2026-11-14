using Libreria.Servicio.Clientes.Application.Ports.Secondary;
using Libreria.Servicio.Clientes.Infrastructure.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// 1. API REST + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. Usuario que opera (puerto IUsuarioActual). Hasta US-127 solo existe el de desarrollo.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IUsuarioActual, UsuarioActualDesarrollo>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapControllers();
app.MapGet("/salud", () => Results.Ok(new { servicio = "Clientes", estado = "ok" }));

app.Run();
