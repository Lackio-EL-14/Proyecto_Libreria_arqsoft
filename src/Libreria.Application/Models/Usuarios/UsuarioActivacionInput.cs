namespace Libreria.Application.Models;

public class UsuarioActivacionInput
{
    public string TokenHash { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string ConfirmarContrasena { get; set; } = string.Empty;
}