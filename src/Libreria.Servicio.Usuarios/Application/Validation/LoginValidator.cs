using Libreria.Servicio.Usuarios.Application.DTOs.Auth;

namespace Libreria.Servicio.Usuarios.Application.Validation;

public sealed class LoginValidator
{
    public void Normalize(LoginRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        request.Username =
            (request.Username ?? string.Empty).Trim();

        request.Password ??= string.Empty;
    }

    public IReadOnlyDictionary<string, string> Validate(
        LoginRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors[nameof(LoginRequest.Username)] =
                "El nombre de usuario es obligatorio.";
        }
        else if (request.Username.Length > 50)
        {
            errors[nameof(LoginRequest.Username)] =
                "El nombre de usuario no puede superar los 50 caracteres.";
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors[nameof(LoginRequest.Password)] =
                "La contraseña es obligatoria.";
        }
        else if (request.Password.Length > 128)
        {
            errors[nameof(LoginRequest.Password)] =
                "La contraseña no puede superar los 128 caracteres.";
        }

        return errors;
    }
}