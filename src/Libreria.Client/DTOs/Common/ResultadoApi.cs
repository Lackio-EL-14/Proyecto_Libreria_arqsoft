namespace Libreria.Client.DTOs.Ventas;

// Resultado de una operación de escritura contra el servicio.
// Las claves de Errores son las mismas que usan las vistas (por ejemplo
// "Input.Nombre"), así que se agregan tal cual al ModelState.
public record ResultadoApi(
    bool Exitoso,
    bool NoEncontrado,
    IReadOnlyDictionary<string, string> Errores)
{
    public static ResultadoApi Correcto() =>
        new(true, false, new Dictionary<string, string>());

    public static ResultadoApi NoExiste() =>
        new(false, true, new Dictionary<string, string>());

    public static ResultadoApi Invalido(IReadOnlyDictionary<string, string> errores) =>
        new(false, false, errores);

    public static ResultadoApi Invalido(string mensaje) =>
        new(false, false, new Dictionary<string, string> { [string.Empty] = mensaje });
}
