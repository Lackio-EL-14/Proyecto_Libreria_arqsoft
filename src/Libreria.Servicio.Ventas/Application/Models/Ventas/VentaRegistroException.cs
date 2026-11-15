namespace Libreria.Servicio.Ventas.Application.Models;

public class VentaRegistroException : InvalidOperationException
{
    public bool ResultadoIncierto { get; }

    public VentaRegistroException(
        string mensaje,
        bool resultadoIncierto = false,
        Exception? innerException = null)
        : base(mensaje, innerException)
    {
        ResultadoIncierto = resultadoIncierto;
    }
}
