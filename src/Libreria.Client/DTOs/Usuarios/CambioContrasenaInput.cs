namespace Libreria.Client.DTOs.Usuarios;

public class CambioContrasenaInput
{
    public string ContrasenaActual { get; set; } = string.Empty;
    public string NuevaContrasena { get; set; } = string.Empty;
    public string Confirmacion { get; set; } = string.Empty;
}
