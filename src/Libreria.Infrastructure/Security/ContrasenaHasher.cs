using Libreria.Application.Domain;
using Libreria.Application.Ports.Secondary;
using Microsoft.AspNetCore.Identity;

namespace Libreria.Infrastructure.Security;

public class ContrasenaHasher : IContrasenaHasher
{
    private readonly PasswordHasher<Usuario> _hasher = new();

    public string Hashear(string contrasena)
    {
        return _hasher.HashPassword(null!, contrasena);
    }

    public bool Verificar(string hash, string contrasena)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(contrasena))
        {
            return false;
        }

        try
        {
            var resultado = _hasher.VerifyHashedPassword(null!, hash, contrasena);
            return resultado != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
