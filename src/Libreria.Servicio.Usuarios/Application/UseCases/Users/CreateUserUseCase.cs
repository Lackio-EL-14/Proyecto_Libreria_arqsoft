using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Comun;
using System.Net.Mail;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Domain.Entities;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class CreateUserUseCase : ICreateUserUseCase
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

    public async Task<Result<CreateUserResultDto>?> ExecuteAsync(
        CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_currentUser.UserId is not Guid currentUserId ||
            !await _userRepository
                .IsActiveAdministratorAsync(currentUserId))
        {
            return null;
        }

        var validation = Validate(request);
        if (!validation.EsExito)
        {
            return Result<CreateUserResultDto>.Fallo(validation);
        }

        var firstName = request.FirstName.Trim();
        var lastName = request.LastName.Trim();
        var email = request.Email.Trim();
        var roleName = request.Role.Trim();

        if (await _userRepository.EmailExistsAsync(email))
        {
            return Result<CreateUserResultDto>.Fallo(
                Result.Conflicto(nameof(request.Email),
                    "Ya existe un usuario registrado con ese correo."));
        }

        var roleId =
            await _userRepository
                .GetActiveRoleIdByNameAsync(roleName);

        if (roleId is null)
        {
            return Result<CreateUserResultDto>.Fallo(
                Result.Validacion(nameof(request.Role),
                    "Debe seleccionar un rol válido."));
        }

        var username = await _usuarioFacade.GenerateUsernameAsync(
            firstName,
            lastName);
        var maternalLastName = string.IsNullOrWhiteSpace(request.MaternalLastName)
            ? null
            : request.MaternalLastName.Trim();

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            MaternalLastName = maternalLastName,
            Email = email,
            Username = username,
            FullName = string.Join(" ", new[] { firstName, lastName, maternalLastName }
                .Where(part => !string.IsNullOrWhiteSpace(part))),
            RoleId = roleId.Value,
            Status = UserStatuses.Pending,
            CreatedByUserId = currentUserId
        };

        var created = await _transactionRunner.ExecuteAsync(
            async () =>
            {
                var publicId = await _userRepository.CreatePendingAsync(user);
                return new CreateUserResultDto(publicId, username);
            });

        return Result<CreateUserResultDto>.Exito(created);
    }

    private static Result Validate(
        CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return Result.Validacion(nameof(request.FirstName),
                "El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Validacion(nameof(request.LastName),
                "El apellido es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result.Validacion(nameof(request.Email),
                "El correo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return Result.Validacion(nameof(request.Role),
                "Debe seleccionar un rol válido.");
        }

        if (!request.FirstName.Trim().All(char.IsLetter))
        {
            return Result.Validacion(nameof(request.FirstName),
                "El nombre solo permite letras, incluyendo acentos y ñ.");
        }

        if (!request.LastName.Trim().All(char.IsLetter))
        {
            return Result.Validacion(nameof(request.LastName),
                "El apellido solo permite letras, incluyendo acentos y ñ.");
        }

        if (!string.IsNullOrWhiteSpace(request.MaternalLastName) &&
            !request.MaternalLastName.Trim().All(char.IsLetter))
        {
            return Result.Validacion(nameof(request.MaternalLastName),
                "El apellido materno solo permite letras, incluyendo acentos y ñ.");
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
            return Result.Validacion(nameof(request.Email),
                "El correo no tiene un formato válido.");
        }

        return Result.Exito();
    }
}