namespace Libreria.Servicio.Usuarios.Application.Validation;

public sealed class PasswordPolicyValidator
{
    public IReadOnlyList<string> Validate(string? password)
    {
        var errors = new List<string>();
        password ??= string.Empty;

        if (password.Length < 8)
        {
            errors.Add(
                "La contraseña debe tener al menos 8 caracteres.");
        }

        if (!password.Any(char.IsUpper))
        {
            errors.Add(
                "La contraseña debe contener al menos una letra mayúscula.");
        }

        if (!password.Any(char.IsLower))
        {
            errors.Add(
                "La contraseña debe contener al menos una letra minúscula.");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add(
                "La contraseña debe contener al menos un número.");
        }

        if (!password.Any(character =>
                char.IsPunctuation(character) ||
                char.IsSymbol(character)))
        {
            errors.Add(
                "La contraseña debe contener al menos un carácter especial.");
        }

        return errors;
    }
}