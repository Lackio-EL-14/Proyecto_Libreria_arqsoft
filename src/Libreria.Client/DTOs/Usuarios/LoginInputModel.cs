using System.ComponentModel.DataAnnotations;

namespace Libreria.Client.DTOs.Usuarios;

public sealed class LoginInputModel
{
    [Required(ErrorMessage = "Ingrese su nombre de usuario.")]
    [StringLength(50, ErrorMessage = "El nombre de usuario no puede superar 50 caracteres.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [StringLength(128, ErrorMessage = "La contraseña no puede superar 128 caracteres.")]
    [DataType(DataType.Password)]
    public string Contrasena { get; set; } = string.Empty;
}
