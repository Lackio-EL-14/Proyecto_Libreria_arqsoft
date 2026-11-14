using Libreria.Gateway.Configuracion;
using Libreria.Gateway.Errores;

var builder = WebApplication.CreateBuilder(args);

// 1. Rutas del gateway: yarp.g1.json (Grupo 1) + yarp.g2.json (Grupo 2); destinos sobrescribibles por entorno.
builder.Configuration.AgregarRutasYarp(args);

// 2. Proxy inverso (YARP). La validación del JWT se agrega en US-126; aquí solo se enruta.
builder.Services.AddProblemDetails();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(RutasYarpExtensions.SeccionProxy));

var app = builder.Build();

app.MapGet("/salud", () => Results.Ok(new { servicio = "Gateway", estado = "ok" }));

app.MapReverseProxy(proxy =>
{
    // Primero el traductor de errores, para que envuelva todo el reenvío.
    proxy.UseMiddleware<ErrorDeProxyMiddleware>();

    // Pasos por defecto de YARP (al personalizar el pipeline hay que declararlos).
    proxy.UseSessionAffinity();
    proxy.UseLoadBalancing();
    proxy.UsePassiveHealthChecks();
});

app.Run();

// Expuesto para las pruebas de integración (WebApplicationFactory).
public partial class Program;
