namespace Libreria.Client.Seguridad;

/// <summary>
/// Un servicio respondió 401 o el token de la sesión ya venció. <see cref="SesionExpiradaFilter"/>
/// la convierte en cierre de sesión y redirección al login con un mensaje (US-125).
/// </summary>
public sealed class SesionExpiradaException : Exception
{
    public SesionExpiradaException(string mensaje)
        : base(mensaje)
    {
    }
}
