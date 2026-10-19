namespace Libreria.Application.Validators;

public class PasswordPolicyValidator
{
    public IReadOnlyList<string> Validar(string? contrasena)
    {
        var errores = new List<string>();
        contrasena ??= string.Empty;

        if (contrasena.Length < 8)
        {
            errores.Add("La contraseña debe tener al menos 8 caracteres.");
        }

        if (!contrasena.Any(char.IsUpper))
        {
            errores.Add("La contraseña debe contener al menos una letra mayúscula.");
        }

        if (!contrasena.Any(char.IsLower))
        {
            errores.Add("La contraseña debe contener al menos una letra minúscula.");
        }

        if (!contrasena.Any(char.IsDigit))
        {
            errores.Add("La contraseña debe contener al menos un número.");
        }

        if (!contrasena.Any(caracter =>
                char.IsPunctuation(caracter) || char.IsSymbol(caracter)))
        {
            errores.Add("La contraseña debe contener al menos un carácter especial.");
        }

        return errores;
    }
}
