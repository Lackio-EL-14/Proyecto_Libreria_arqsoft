using System.ComponentModel.DataAnnotations;

namespace Libreria.Client.DTOs.Usuarios;

public sealed class ActivateUserInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Ingrese una contraseña.")]
    [StringLength(128, ErrorMessage = "La contraseña no puede superar 128 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme la contraseña.")]
    [DataType(DataType.Password)]
    public string Confirmation { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Password))
        {
            yield break;
        }

        if (Password.Length < 8)
        {
            yield return new ValidationResult(
                "La contraseña debe tener al menos 8 caracteres.",
                [nameof(Password)]);
        }

        if (!Password.Any(char.IsUpper))
        {
            yield return new ValidationResult(
                "La contraseña debe contener al menos una letra mayúscula.",
                [nameof(Password)]);
        }

        if (!Password.Any(char.IsLower))
        {
            yield return new ValidationResult(
                "La contraseña debe contener al menos una letra minúscula.",
                [nameof(Password)]);
        }

        if (!Password.Any(char.IsDigit))
        {
            yield return new ValidationResult(
                "La contraseña debe contener al menos un número.",
                [nameof(Password)]);
        }

        if (!Password.Any(character =>
                char.IsPunctuation(character) ||
                char.IsSymbol(character)))
        {
            yield return new ValidationResult(
                "La contraseña debe contener al menos un carácter especial.",
                [nameof(Password)]);
        }

        if (!string.IsNullOrEmpty(Confirmation) &&
            !string.Equals(Password, Confirmation, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "La confirmación no coincide con la contraseña.",
                [nameof(Confirmation)]);
        }
    }
}
