using Libreria.Servicio.Usuarios.Application.Comun;
using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.DTOs.Auth;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Auth;

// TRANSITORIO (US-124 por etapas): se retira en US-125, cuando el Front inicie sesión con OIDC
// contra Keycloak. Ver docs/contratos/keycloak.md, «Retiro del login transitorio».
public sealed class LoginUseCase : ILoginUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly LoginValidator _validator;

    public LoginUseCase(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        LoginValidator validator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _validator = validator;
    }

    public async Task<Result<AuthenticatedUserDto>> ExecuteAsync(
        LoginRequest request)
    {
        _validator.Normalize(request);

        var errors = _validator.Validate(request);

        if (errors.Count > 0)
        {
            return Result<AuthenticatedUserDto>.Fallo(
                Result.Validacion(errors));
        }

        var user = await _userRepository
            .GetActiveByUsernameAsync(request.Username);

        if (user is null ||
            user.Status != UserStatuses.Active ||
            !user.Role.IsActive ||
            !_passwordHasher.Verify(
                user.PasswordHash,
                request.Password))
        {
            return Result<AuthenticatedUserDto>.Fallo(
                Result.Validacion("Credenciales", "Usuario o contraseña incorrectos."));
        }

        await TryRehashPasswordAsync(
            user.PublicId,
            user.PasswordHash,
            request.Password);

        return Result<AuthenticatedUserDto>.Exito(new AuthenticatedUserDto(
            user.Id,
            user.PublicId,
            user.Username,
            user.FullName,
            user.Role.Name));
    }

    private async Task TryRehashPasswordAsync(
        Guid userId,
        string currentHash,
        string password)
    {
        if (!_passwordHasher.RequiresRehash(currentHash))
        {
            return;
        }

        try
        {
            var newHash = _passwordHasher.Hash(password);

            await _userRepository.UpdatePasswordAsync(
                userId,
                currentHash,
                newHash);
        }
        catch
        {
            // La migración del hash es oportunista:
            // un fallo aquí no debe impedir un login válido.
        }
    }
}