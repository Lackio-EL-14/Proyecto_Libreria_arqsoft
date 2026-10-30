using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.DTOs.Auth;
using Libreria.Servicio.Usuarios.Application.Validation;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Auth;

public sealed class LoginUseCase
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

    public async Task<AuthenticatedUserDto?> ExecuteAsync(
        LoginRequest request)
    {
        _validator.Normalize(request);

        var errors = _validator.Validate(request);

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                string.Join(" ", errors.Values));
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
            return null;
        }

        await TryRehashPasswordAsync(
             user.Id,
             user.PasswordHash,
             request.Password);

        return new AuthenticatedUserDto(
            user.Id,
            user.PublicId,
            user.Username,
            user.FullName,
            user.Role.Name);
    }

    private async Task TryRehashPasswordAsync(
    int userId,
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