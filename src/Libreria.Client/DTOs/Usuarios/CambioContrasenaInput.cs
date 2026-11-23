using System.ComponentModel.DataAnnotations;

namespace Libreria.Client.DTOs.Usuarios;

/// <summary>
/// Formulario de cambio de contraseña (US-129). La política se valida aquí (servidor del Front),
/// en <c>cambiar-contrasena.js</c> (en vivo) y en el MS Usuarios / Keycloak.
/// </summary>
public class CambioContrasenaInput
{
    /// <summary>Mínimo 8 caracteres con mayúscula, minúscula, número y carácter especial.</summary>
    public const string PatronPolitica = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,128}$";

    [Display(Name = "Contraseña actual")]
    [Required(ErrorMessage = "Ingrese su contraseña actual.")]
    [StringLength(128, ErrorMessage = "La contraseña no puede superar los 128 caracteres.")]
    public string ContrasenaActual { get; set; } = string.Empty;

    [Display(Name = "Nueva contraseña")]
    [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
    [RegularExpression(PatronPolitica, ErrorMessage =
        "La nueva contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.")]
    public string NuevaContrasena { get; set; } = string.Empty;

    [Display(Name = "Confirme la nueva contraseña")]
    [Required(ErrorMessage = "Ingrese la confirmación de la nueva contraseña.")]
    [Compare(nameof(NuevaContrasena), ErrorMessage = "La confirmación no coincide con la nueva contraseña.")]
    public string Confirmacion { get; set; } = string.Empty;
}
