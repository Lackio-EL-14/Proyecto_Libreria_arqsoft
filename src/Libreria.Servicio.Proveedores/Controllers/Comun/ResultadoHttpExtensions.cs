using Libreria.Servicio.Proveedores.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Proveedores.Controllers.Comun;

public static class ResultadoHttpExtensions
{
    public static IActionResult AActionResult(this Result resultado, Func<IActionResult>? siExito = null) =>
        resultado.EsExito ? siExito?.Invoke() ?? new NoContentResult() : Problema(resultado);
    public static IActionResult AActionResult<T>(this Result<T> resultado, Func<T, IActionResult> siExito) =>
        resultado.EsExito ? siExito(resultado.Valor) : Problema(resultado);

    private static ObjectResult Problema(Result resultado)
    {
        ProblemDetails detalle = resultado.Tipo switch
        {
            TipoError.Validacion => new ValidationProblemDetails(Errores(resultado))
            { Status = 400, Title = "Uno o más datos no son válidos." },
            TipoError.NoEncontrado => new ProblemDetails
            { Status = 404, Title = "No se encontró el recurso.", Detail = Mensaje(resultado) },
            TipoError.Conflicto => new ValidationProblemDetails(Errores(resultado))
            { Status = 409, Title = "La operación entra en conflicto con el estado actual.", Detail = Mensaje(resultado) },
            _ => throw new InvalidOperationException("Un resultado exitoso no contiene un problema.")
        };
        return new ObjectResult(detalle) { StatusCode = detalle.Status };
    }
    private static Dictionary<string, string[]> Errores(Result r) => r.Errores.ToDictionary(x => x.Key, x => new[] { x.Value });
    private static string? Mensaje(Result r) => r.Errores.Values.FirstOrDefault();
}
