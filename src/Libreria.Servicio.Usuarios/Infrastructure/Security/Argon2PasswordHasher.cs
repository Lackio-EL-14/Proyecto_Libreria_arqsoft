using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Libreria.Servicio.Usuarios.Infrastructure.Security;

public sealed class Argon2PasswordHasher : IPasswordHasher
{
    private const int MemorySize = 65_536;
    private const int Iterations = 3;
    private const int Parallelism = 2;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    private readonly PasswordHasher<User> _legacyHasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = Derive(
            password,
            salt,
            MemorySize,
            Iterations,
            Parallelism,
            HashSize);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"$argon2id$v=19$m={MemorySize},t={Iterations},p={Parallelism}${Encode(salt)}${Encode(hash)}");
    }

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrEmpty(hash) ||
            string.IsNullOrEmpty(password))
        {
            return false;
        }

        try
        {
            if (TryParseArgon2(hash, out var parameters))
            {
                var calculated = Derive(
                    password,
                    parameters.Salt,
                    parameters.MemorySize,
                    parameters.Iterations,
                    parameters.Parallelism,
                    parameters.Hash.Length);

                return CryptographicOperations.FixedTimeEquals(
                    calculated,
                    parameters.Hash);
            }

            var result = _legacyHasher.VerifyHashedPassword(
                null!,
                hash,
                password);

            return result != PasswordVerificationResult.Failed;
        }
        catch (Exception exception) when (
            exception is FormatException
                or ArgumentException
                or CryptographicException)
        {
            return false;
        }
    }

    public bool RequiresRehash(string hash)
    {
        return !TryParseArgon2(hash, out var parameters)
            || parameters.MemorySize != MemorySize
            || parameters.Iterations != Iterations
            || parameters.Parallelism != Parallelism
            || parameters.Salt.Length != SaltSize
            || parameters.Hash.Length != HashSize;
    }

    private static byte[] Derive(
        string password,
        byte[] salt,
        int memorySize,
        int iterations,
        int parallelism,
        int hashSize)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            using var argon2 = new Argon2id(passwordBytes)
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
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static bool TryParseArgon2(
        string hash,
        out Argon2Parameters parameters)
    {
        parameters = default;

        var parts = hash.Split('$', StringSplitOptions.None);

        if (parts.Length != 6
            || parts[0].Length != 0
            || parts[1] != "argon2id"
            || parts[2] != "v=19")
        {
            return false;
        }

        var values = parts[3]
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (values.Length != 3
            || !TryReadParameter(values[0], "m", out var memorySize)
            || !TryReadParameter(values[1], "t", out var iterations)
            || !TryReadParameter(values[2], "p", out var parallelism)
            || memorySize is < 8_192 or > 262_144
            || iterations is < 1 or > 10
            || parallelism is < 1 or > 8)
        {
            return false;
        }

        try
        {
            var salt = Decode(parts[4]);
            var hashBytes = Decode(parts[5]);

            if (salt.Length is < 16 or > 64 ||
                hashBytes.Length is < 16 or > 64)
            {
                return false;
            }

            parameters = new Argon2Parameters(
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
        string text,
        string name,
        out int value)
    {
        value = 0;

        var parts = text.Split('=', 2);

        return parts.Length == 2
            && parts[0] == name
            && int.TryParse(
                parts[1],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value);
    }

    private static string Encode(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=');
    }

    private static byte[] Decode(string value)
    {
        var padding = (4 - value.Length % 4) % 4;

        return Convert.FromBase64String(
            value + new string('=', padding));
    }

    private readonly record struct Argon2Parameters(
        int MemorySize,
        int Iterations,
        int Parallelism,
        byte[] Salt,
        byte[] Hash);
}