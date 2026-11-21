using System.Text.Json;
using Libreria.Servicio.Usuarios.Presentation.Comun;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Libreria.Servicio.Usuarios.Tests.Comun;

/// <summary>US-139: excepciones inesperadas → ProblemDetails 503/500 sin detalles internos.</summary>
public class ManejadorExcepcionesTests
{
    [Fact]
    public async Task Keycloak_sin_respuesta_devuelve_503()
    {
        var (status, cuerpo) = await ManejarAsync(new HttpRequestException("Connection refused (localhost:8080)"));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("El servicio de Usuarios no está disponible.", cuerpo.GetProperty("title").GetString());
        Assert.DoesNotContain("Connection refused", cuerpo.GetRawText());
    }

    [Fact]
    public async Task Error_inesperado_devuelve_500_sin_detalles_internos()
    {
        var (status, cuerpo) = await ManejarAsync(new InvalidOperationException("secreto interno"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
        Assert.Equal("Ocurrió un error inesperado.", cuerpo.GetProperty("title").GetString());
        Assert.DoesNotContain("secreto interno", cuerpo.GetRawText());
    }

    private static async Task<(int Status, JsonElement Cuerpo)> ManejarAsync(Exception excepcion)
    {
        var servicios = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();

        var contexto = new DefaultHttpContext { RequestServices = servicios };
        contexto.Response.Body = new MemoryStream();

        var manejador = new ManejadorExcepciones(
            servicios.GetRequiredService<IProblemDetailsService>(),
            NullLogger<ManejadorExcepciones>.Instance);

        Assert.True(await manejador.TryHandleAsync(contexto, excepcion, CancellationToken.None));

        contexto.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        return (contexto.Response.StatusCode, documento.RootElement.Clone());
    }
}
