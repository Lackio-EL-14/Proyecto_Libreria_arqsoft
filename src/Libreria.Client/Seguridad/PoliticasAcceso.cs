namespace Libreria.Client.Seguridad;

// Nombres de las políticas de autorización (US-39). Se usan tanto en
// Program.cs (validación en el servidor) como en el layout (qué menú se ve),
// así la regla de "quién puede entrar" vive en un solo lugar.
public static class PoliticasAcceso
{
    public const string SoloAdministrador = "SoloAdministrador";
    public const string Ventas = "Ventas";
}
