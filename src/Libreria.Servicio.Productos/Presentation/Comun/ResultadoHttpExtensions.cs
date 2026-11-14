using Libreria.Servicio.Productos.Application.Comun;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Productos.Presentation.Comun;

/// <summary>
/// Adaptador de entrada: traduce un <see cref="Result"/> de la capa de aplicación
/// a una respuesta HTTP (contrato 6.5): Validación → 400, NoEncontrado → 404, Conflicto → 409.
/// </summary>
public static class ResultadoHttpExtensions
{
    /// <summary>Si el resultado es exitoso devuelve <paramref name="siExito"/> (204 por defecto); si no, su ProblemDetails.</summary>
    public static IActionResult AActionResult(this Result resultado, Func<IActionResult>? siExito = null) =>
        resultado.EsExito
            ? siExito?.Invoke() ?? new NoContentResult()
            : AProblemDetails(resultado);

    /// <summary>Si el resultado es exitoso entrega su valor a <paramref name="siExito"/>; si no, su ProblemDetails.</summary>
    public static IActionResult AActionResult<T>(this Result<T> resultado, Func<T, IActionResult> siExito) =>
        resultado.EsExito
            ? siExito(resultado.Valor)
            : AProblemDetails(resultado);

    /// <summary>Construye la respuesta de error. No debe llamarse con un resultado exitoso.</summary>
    public static ObjectResult AProblemDetails(this Result resultado)
    {
        ProblemDetails problema = resultado.Tipo switch
        {
            TipoError.Validacion => new ValidationProblemDetails(AgruparErrores(resultado))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Uno o más datos no son válidos."
            },
            TipoError.NoEncontrado => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "No se encontró el recurso.",
                Detail = PrimerMensaje(resultado)
            },
            TipoError.Conflicto => new ValidationProblemDetails(AgruparErrores(resultado))
            {
                Status = StatusCodes.Status409Conflict,
                Title = "La operación entra en conflicto con el estado actual.",
                Detail = PrimerMensaje(resultado)
            },
            _ => throw new InvalidOperationException("Un resultado exitoso no tiene ProblemDetails.")
        };

        return new ObjectResult(problema) { StatusCode = problema.Status };
    }

    private static Dictionary<string, string[]> AgruparErrores(Result resultado) =>
        resultado.Errores.ToDictionary(error => error.Key, error => new[] { error.Value });

    private static string? PrimerMensaje(Result resultado) =>
        resultado.Errores.Values.FirstOrDefault();
}
