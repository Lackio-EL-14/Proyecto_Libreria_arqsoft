using System.Security.Cryptography;
using System.Text;

namespace Libreria.Application.Services;

public static class TokenActivacionService
{
    public static string GenerarToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string CalcularHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}