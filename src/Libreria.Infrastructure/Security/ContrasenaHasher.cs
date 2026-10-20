using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;
using Microsoft.AspNetCore.Identity;

namespace Libreria.Infrastructure.Security;

public sealed class ContrasenaHasher : IContrasenaHasher
{
    private const int MemorySize = 65_536;
    private const int Iterations = 3;
    private const int Parallelism = 2;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly PasswordHasher<Usuario> _legacyHasher = new();

    public string Hashear(string contrasena)
    {
        ArgumentException.ThrowIfNullOrEmpty(contrasena);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derivar(
            contrasena,
            salt,
            MemorySize,
            Iterations,
            Parallelism,
            HashSize);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"$argon2id$v=19$m={MemorySize},t={Iterations},p={Parallelism}${Codificar(salt)}${Codificar(hash)}");
    }

    public bool Verificar(string hash, string contrasena)
    {
        if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(contrasena))
        {
            return false;
        }

        try
        {
            if (TryParseArgon2(hash, out var parametros))
            {
                var calculado = Derivar(
                    contrasena,
                    parametros.Salt,
                    parametros.MemorySize,
                    parametros.Iterations,
                    parametros.Parallelism,
                    parametros.Hash.Length);

                return CryptographicOperations.FixedTimeEquals(
                    calculado,
                    parametros.Hash);
            }

            var resultado = _legacyHasher.VerifyHashedPassword(
                null!,
                hash,
                contrasena);

            return resultado != PasswordVerificationResult.Failed;
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException or CryptographicException)
        {
            return false;
        }
    }

    public bool RequiereRehash(string hash)
    {
        return !TryParseArgon2(hash, out var parametros)
            || parametros.MemorySize != MemorySize
            || parametros.Iterations != Iterations
            || parametros.Parallelism != Parallelism
            || parametros.Salt.Length != SaltSize
            || parametros.Hash.Length != HashSize;
    }

    private static byte[] Derivar(
        string contrasena,
        byte[] salt,
        int memorySize,
        int iterations,
        int parallelism,
        int hashSize)
    {
        var bytesContrasena = Encoding.UTF8.GetBytes(contrasena);

        try
        {
            using var argon2 = new Argon2id(bytesContrasena)
            {
                Salt = salt,
                MemorySize = memorySize,
                Iterations = iterations,
                DegreeOfParallelism = parallelism
            };

            return argon2.GetBytes(hashSize);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytesContrasena);
        }
    }

    private static bool TryParseArgon2(
        string hash,
        out Argon2Parameters parametros)
    {
        parametros = default;
        var partes = hash.Split('$', StringSplitOptions.None);

        if (partes.Length != 6
            || partes[0].Length != 0
            || partes[1] != "argon2id"
            || partes[2] != "v=19")
        {
            return false;
        }

        var valores = partes[3].Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (valores.Length != 3
            || !TryReadParameter(valores[0], "m", out var memorySize)
            || !TryReadParameter(valores[1], "t", out var iterations)
            || !TryReadParameter(valores[2], "p", out var parallelism)
            || memorySize is < 8_192 or > 262_144
            || iterations is < 1 or > 10
            || parallelism is < 1 or > 8)
        {
            return false;
        }

        try
        {
            var salt = Decodificar(partes[4]);
            var hashBytes = Decodificar(partes[5]);

            if (salt.Length is < 16 or > 64 || hashBytes.Length is < 16 or > 64)
            {
                return false;
            }

            parametros = new Argon2Parameters(
                memorySize,
                iterations,
                parallelism,
                salt,
                hashBytes);

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryReadParameter(
        string texto,
        string nombre,
        out int valor)
    {
        valor = 0;
        var partes = texto.Split('=', 2);

        return partes.Length == 2
            && partes[0] == nombre
            && int.TryParse(
                partes[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out valor);
    }

    private static string Codificar(byte[] valor)
    {
        return Convert.ToBase64String(valor).TrimEnd('=');
    }

    private static byte[] Decodificar(string valor)
    {
        var padding = (4 - valor.Length % 4) % 4;
        return Convert.FromBase64String(valor + new string('=', padding));
    }

    private readonly record struct Argon2Parameters(
        int MemorySize,
        int Iterations,
        int Parallelism,
        byte[] Salt,
        byte[] Hash);
}
