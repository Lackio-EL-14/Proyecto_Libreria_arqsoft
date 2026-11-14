var builder = WebApplication.CreateBuilder(args);

// El enrutamiento con YARP (yarp.g1.json y yarp.g2.json) se agrega en US-112.

var app = builder.Build();

app.MapGet("/salud", () => Results.Ok(new { servicio = "Gateway", estado = "ok" }));

app.Run();
