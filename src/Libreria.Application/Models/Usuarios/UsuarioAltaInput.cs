namespace Libreria.Application.Models;

public class UsuarioAltaInput
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int RolId { get; set; }
}