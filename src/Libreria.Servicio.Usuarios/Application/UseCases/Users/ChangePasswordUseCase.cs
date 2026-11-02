using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Validation;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class ChangePasswordUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordPolicyValidator _passwordPolicy;

    public ChangePasswordUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        PasswordPolicyValidator passwordPolicy)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
    }

    public async Task<bool> ExecuteAsync(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = _currentUser.UserId;

        if (userId is not > 0)
        {
            return false;
        }

        if (string.IsNullOrEmpty(request.CurrentPassword))
        {
            throw new ArgumentException(
                "Ingrese su contraseña actual.");
        }

        if (!string.Equals(
                request.NewPassword,
                request.Confirmation,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La confirmación no coincide con la nueva contraseña.");
        }

        var policyErrors =
            _passwordPolicy.Validate(request.NewPassword);

        if (policyErrors.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", policyErrors));
        }

        if (request.NewPassword.Length > 128)
        {
            throw new ArgumentException(
                "La contraseña no puede superar los 128 caracteres.");
        }

        var currentHash =
            await _userRepository.GetActivePasswordHashAsync(
                userId.Value);

        if (currentHash is null ||
            !_passwordHasher.Verify(
                currentHash,
                request.CurrentPassword))
        {
            throw new ArgumentException(
                "La contraseña actual es incorrecta.");
        }

        if (string.Equals(
                request.NewPassword,
                request.CurrentPassword,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "La nueva contraseña debe ser distinta de la actual.");
        }

        var newHash =
            _passwordHasher.Hash(request.NewPassword);

        var updated =
            await _userRepository.UpdatePasswordAsync(
                userId.Value,
                currentHash,
                newHash);

        if (!updated)
        {
            throw new InvalidOperationException(
                "No fue posible cambiar la contraseña. Intente nuevamente.");
        }

        return true;
    }
}
