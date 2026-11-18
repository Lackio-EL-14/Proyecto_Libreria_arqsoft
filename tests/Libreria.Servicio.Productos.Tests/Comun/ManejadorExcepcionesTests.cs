using System.Text.Json;
using Libreria.Servicio.Productos.Presentation.Comun;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Libreria.Servicio.Productos.Tests.Comun;

public class ManejadorExcepcionesTests
{
    [Fact]
    public async Task Mongo_sin_respuesta_devuelve_503_con_ProblemDetails()
    {
        var (status, cuerpo) = await ManejarAsync(new TimeoutException("Server selection timeout."));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, status);
        Assert.Equal("El servicio de Productos no está disponible.", cuerpo.GetProperty("title").GetString());
        Assert.DoesNotContain("Server selection", cuerpo.GetRawText());
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
            servicios.GetRequiredService<Microsoft.AspNetCore.Http.IProblemDetailsService>(),
            NullLogger<ManejadorExcepciones>.Instance);

        Assert.True(await manejador.TryHandleAsync(contexto, excepcion, CancellationToken.None));

        contexto.Response.Body.Position = 0;
        using var documento = await JsonDocument.ParseAsync(contexto.Response.Body);
        return (contexto.Response.StatusCode, documento.RootElement.Clone());
    }
}
