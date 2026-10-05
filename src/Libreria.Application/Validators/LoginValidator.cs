using Libreria.Application.Models;

namespace Libreria.Application.Validators;

public class LoginValidator
{
    public void Normalizar(LoginInput input)
    {
        input.NombreUsuario = (input.NombreUsuario ?? string.Empty).Trim();
        input.Contrasena ??= string.Empty;
    }

    public IReadOnlyDictionary<string, string> Validar(LoginInput input)
    {
        var errores = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(input.NombreUsuario))
        {
            errores["Input.NombreUsuario"] = "El nombre de usuario es obligatorio.";
        }
        else if (input.NombreUsuario.Length > 50)
        {
            errores["Input.NombreUsuario"] = "El nombre de usuario no puede superar los 50 caracteres.";
        }

        if (string.IsNullOrEmpty(input.Contrasena))
        {
            errores["Input.Contrasena"] = "La contraseña es obligatoria.";
        }
        else if (input.Contrasena.Length > 128)
        {
            errores["Input.Contrasena"] = "La contraseña no puede superar los 128 caracteres.";
        }

        return errores;
    }
}
