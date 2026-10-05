namespace Libreria.Application.Models;

public record ResultadoAutenticacion(
    bool Exitoso,
    UsuarioAutenticado? Usuario,
    IReadOnlyDictionary<string, string> Errores)
{
    public const string MensajeCredencialesInvalidas =
        "Usuario o contraseña incorrectos.";

    public static ResultadoAutenticacion Correcto(UsuarioAutenticado usuario) =>
        new(true, usuario, new Dictionary<string, string>());

    public static ResultadoAutenticacion Invalido(IReadOnlyDictionary<string, string> errores) =>
        new(false, null, errores);

    public static ResultadoAutenticacion CredencialesInvalidas() =>
        new(false, null, new Dictionary<string, string>
        {
            [string.Empty] = MensajeCredencialesInvalidas
        });
}
