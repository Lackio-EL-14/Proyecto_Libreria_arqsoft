using Libreria.Gateway.Configuracion;
using Libreria.Gateway.Errores;
using Libreria.Gateway.Seguridad;

var builder = WebApplication.CreateBuilder(args);

// 1. Rutas del gateway: yarp.g1.json (Grupo 1) + yarp.g2.json (Grupo 2); destinos sobrescribibles por
//    appsettings.{Entorno}.json, variables de entorno o línea de comandos.
builder.Configuration.AgregarRutasYarp(args, builder.Environment.EnvironmentName);

// 2. Validación del JWT de Keycloak (US-126): sin token válido, el gateway responde 401 y no reenvía.
//    Los roles los aplica cada microservicio, que también vuelve a validar el token.
builder.Services.AddSeguridadJwt(builder.Configuration, builder.Environment);

// 3. Proxy inverso (YARP).
builder.Services.AddProblemDetails();
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(RutasYarpExtensions.SeccionProxy));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/salud", () => Results.Ok(new { servicio = "Gateway", estado = "ok" }))
    .AllowAnonymous();

app.MapReverseProxy(proxy =>
{
    // Primero el traductor de errores, para que envuelva todo el reenvío.
    proxy.UseMiddleware<ErrorDeProxyMiddleware>();

    // Pasos por defecto de YARP (al personalizar el pipeline hay que declararlos).
    proxy.UseSessionAffinity();
    proxy.UseLoadBalancing();
    proxy.UsePassiveHealthChecks();
}).RequireAuthorization();

app.Run();

// Expuesto para las pruebas de integración (WebApplicationFactory).
public partial class Program;
