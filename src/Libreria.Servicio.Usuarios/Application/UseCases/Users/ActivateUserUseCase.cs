using System.Security.Cryptography;
using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class ActivateUserUseCase
{
    private const string InvalidActivationMessage =
        "El enlace de activación no es válido o ha expirado.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordPolicyValidator _passwordPolicy;

    public ActivateUserUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        PasswordPolicyValidator passwordPolicy)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
    }

    public async Task ExecuteAsync(
        ActivateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TokenHash) ||
            string.IsNullOrWhiteSpace(request.Token))
        {
            throw new InvalidOperationException(
                InvalidActivationMessage);
        }

        var user =
            await _userRepository
                .GetByActivationTokenHashAsync(
                    request.TokenHash.Trim());

        var now = DateTime.UtcNow;

        if (user is null ||
            user.Status != UserStatuses.Pending ||
            user.ActivationTokenUsedAt is not null ||
            user.ActivationTokenExpiresAt is null ||
            user.ActivationTokenExpiresAt <= now ||
            string.IsNullOrWhiteSpace(
                user.ActivationTokenHash))
        {
            throw new InvalidOperationException(
                InvalidActivationMessage);
        }

        var calculatedHash =
            ActivationTokenService.CalculateHash(
                request.Token);

        if (!SecureEquals(
                user.ActivationTokenHash,
                calculatedHash))
        {
            throw new InvalidOperationException(
                InvalidActivationMessage);
        }

        if (!string.Equals(
                request.Password,
                request.ConfirmPassword,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La contraseña y su confirmación no coinciden.");
        }

        var passwordErrors =
            _passwordPolicy.Validate(request.Password);

        if (passwordErrors.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", passwordErrors));
        }

        if (request.Password.Length > 128)
        {
            throw new ArgumentException(
                "La contraseña no puede superar los 128 caracteres.");
        }

        var passwordHash =
            _passwordHasher.Hash(request.Password);

        var activated =
            await _userRepository.ActivateAsync(
                user.Id,
                passwordHash,
                now);

        if (!activated)
        {
            throw new InvalidOperationException(
                InvalidActivationMessage);
        }
    }

    private static bool SecureEquals(
        string expectedHash,
        string calculatedHash)
    {
        try
        {
            var expected =
                Convert.FromHexString(expectedHash);

            var calculated =
                Convert.FromHexString(calculatedHash);

            return CryptographicOperations.FixedTimeEquals(
                expected,
                calculated);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}