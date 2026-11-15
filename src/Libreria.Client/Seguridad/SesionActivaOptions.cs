namespace Libreria.Client.Seguridad;

public sealed class SesionActivaOptions
{
    public const string SectionName = "Sesion";

    public int ValidacionEstadoSegundos { get; init; } = 20;
}
