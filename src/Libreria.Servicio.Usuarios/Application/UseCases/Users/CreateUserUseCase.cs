using System.Net.Mail;
using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Domain.Entities;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class CreateUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly UsuarioFacade _usuarioFacade;
    private readonly ITransactionRunner _transactionRunner;

    public CreateUserUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        UsuarioFacade usuarioFacade,
        ITransactionRunner transactionRunner)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _usuarioFacade = usuarioFacade;
        _transactionRunner = transactionRunner;
    }

    public async Task<CreateUserResultDto?> ExecuteAsync(
        CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUser.UserId is not int currentUserId ||
            !await _userRepository
                .IsActiveAdministratorAsync(currentUserId))
        {
            return null;
        }

        Validate(request);

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = request.Email.Trim();
        var roleName = request.Role.Trim();

        if (await _userRepository.EmailExistsAsync(email))
        {
            throw new InvalidOperationException(
                "Ya existe un usuario registrado con ese correo.");
        }

        var roleId =
            await _userRepository
                .GetActiveRoleIdByNameAsync(roleName);

        if (roleId is null)
        {
            throw new ArgumentException(
                "Debe seleccionar un rol válido.",
                nameof(request));
        }

        var activation =
            await _usuarioFacade.PrepareActivationAsync(
                firstName,
                lastName);

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Username = activation.Username,
            FullName = $"{firstName} {lastName}",
            PasswordHash = string.Empty,
            RoleId = roleId.Value,
            Status = UserStatuses.Pending,
            ActivationTokenHash = activation.TokenHash,
            ActivationTokenExpiresAt = activation.ExpiresAt,
            ActivationTokenUsedAt = null,
            CreatedByUserId = currentUserId
        };

        try
        {
            return await _transactionRunner.ExecuteAsync(
                async () =>
                {
                    var publicId =
                        await _userRepository
                            .CreatePendingAsync(user);

                    await _usuarioFacade
                        .SendActivationEmailAsync(
                            email,
                            firstName,
                            activation);

                    return new CreateUserResultDto(
                        publicId,
                        activation.Username);
                });
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "No fue posible completar el alta del usuario.",
                exception);
        }
    }

    private static void Validate(
        CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            throw new ArgumentException(
                "El nombre es obligatorio.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            throw new ArgumentException(
                "El apellido es obligatorio.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException(
                "El correo es obligatorio.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            throw new ArgumentException(
                "Debe seleccionar un rol válido.",
                nameof(request));
        }

        if (!request.FirstName.Trim().All(char.IsLetter))
        {
            throw new ArgumentException(
                "El nombre solo permite letras, incluyendo acentos y ñ.",
                nameof(request));
        }

        if (!request.LastName.Trim().All(char.IsLetter))
        {
            throw new ArgumentException(
                "El apellido solo permite letras, incluyendo acentos y ñ.",
                nameof(request));
        }

        try
        {
            var address =
                new MailAddress(request.Email.Trim());

            if (!string.Equals(
                    address.Address,
                    request.Email.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                "El correo no tiene un formato válido.",
                nameof(request));
        }
    }
}