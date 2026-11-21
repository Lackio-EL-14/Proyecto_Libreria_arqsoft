using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Domain;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

/// <summary>
/// Cambio de contraseña del usuario actual (US-129), con las credenciales en Keycloak (US-125):
/// valida la entrada, comprueba la contraseña actual y fija la nueva como definitiva por medio
/// de <see cref="IProveedorIdentidad"/>. Las validaciones vuelven como <see cref="Result"/>.
/// </summary>
public sealed class ChangePasswordUseCase : IChangePasswordUseCase
{
    private const int LongitudMaxima = 128;

    private readonly IProveedorIdentidad _proveedorIdentidad;
    private readonly ICurrentUser _currentUser;
    private readonly PasswordPolicyValidator _passwordPolicy;
    private readonly IUserRepository _userRepository;

    public ChangePasswordUseCase(
        IProveedorIdentidad proveedorIdentidad,
        ICurrentUser currentUser,
        PasswordPolicyValidator passwordPolicy,
        IUserRepository userRepository)
    {
        _proveedorIdentidad = proveedorIdentidad;
        _currentUser = currentUser;
        _passwordPolicy = passwordPolicy;
        _userRepository = userRepository;
    }

    public async Task<Result?> ExecuteAsync(ChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUser.UserId is not Guid keycloakId ||
            keycloakId == Guid.Empty ||
            string.IsNullOrWhiteSpace(_currentUser.Username))
        {
            return null;
        }

        var validacion = Validar(request, _currentUser.Username);
        if (validacion.IsError)
        {
            return validacion;
        }

        var verificacion = await _proveedorIdentidad.VerificarContrasenaAsync(
            _currentUser.Username,
            request.CurrentPassword);

        if (verificacion.IsError)
        {
            return verificacion;
        }

        var cambio = await _proveedorIdentidad.FijarContrasenaAsync(
            keycloakId,
            request.NewPassword,
            temporal: false);

        if (cambio.IsSuccess)
        {
            // Bitácora (US-127/129): el propio usuario queda como autor de la modificación.
            await _userRepository.RegisterPasswordChangeAsync(keycloakId);
        }

        return cambio;
    }

    private Result Validar(ChangePasswordRequest request, string nombreUsuario)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword))
        {
            return Result.Validacion("ContrasenaActual", "Ingrese su contraseña actual.");
        }

        if (!string.Equals(request.NewPassword, request.Confirmation, StringComparison.Ordinal))
        {
            return Result.Validacion("Confirmacion", "La confirmación no coincide con la nueva contraseña.");
        }

        var errores = _passwordPolicy.Validate(request.NewPassword);
        if (errores.Count > 0)
        {
            return Result.Validacion("NuevaContrasena", string.Join(" ", errores));
        }

        if (request.NewPassword.Length > LongitudMaxima)
        {
            return Result.Validacion("NuevaContrasena", "La contraseña no puede superar los 128 caracteres.");
        }

        if (string.Equals(request.NewPassword, nombreUsuario, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Validacion("NuevaContrasena", "La contraseña no puede ser igual al nombre de usuario.");
        }

        if (string.Equals(request.NewPassword, request.CurrentPassword, StringComparison.Ordinal))
        {
            return Result.Validacion("NuevaContrasena", "La nueva contraseña debe ser distinta de la actual.");
        }

        return Result.Exito();
    }
}
