using System.Net.Mail;
using Libreria.Servicio.Usuarios.Application.Comun;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
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

    public async Task<Result<CreateUserResultDto>?> ExecuteAsync(CreateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await IsAuthorizedAdministratorAsync())
        {
            return null;
        }

        var validation = Validate(request);
        if (!validation.EsExito)
        {
            return Result<CreateUserResultDto>.Fallo(validation);
        }

        return await ProcessRegistrationAsync(request);
    }

    private async Task<bool> IsAuthorizedAdministratorAsync()
    {
        if (_currentUser.UserId is not Guid currentUserId)
        {
            return false;
        }

        // Token válido ⇒ ya cambió su contraseña temporal: si seguía «Pendiente», se activa.
        await _userRepository.ActivatePendingAsync(currentUserId);
        return await _userRepository.IsActiveAdministratorAsync(currentUserId);
    }

    private async Task<Result<CreateUserResultDto>> ProcessRegistrationAsync(CreateUserRequest request)
    {
        var normalizedEmail = request.Email.Trim();

        if (await _userRepository.EmailExistsAsync(normalizedEmail))
        {
            return Result<CreateUserResultDto>.Fallo(
                Result.Conflicto(nameof(request.Email), "Ya existe un usuario registrado con ese correo."));
        }

        var roleId = await _userRepository.GetActiveRoleIdByNameAsync(request.Role.Trim());
        if (roleId is null)
        {
            return Result<CreateUserResultDto>.Fallo(
                Result.Validacion(nameof(request.Role), "Debe seleccionar un rol válido."));
        }

        return await RegisterUserAsync(request, roleId.Value, normalizedEmail);
    }

    private async Task<Result<CreateUserResultDto>> RegisterUserAsync(
        CreateUserRequest request,
        int roleId,
        string normalizedEmail)
    {
        var firstName = NormalizeName(request.FirstName);
        var lastName = NormalizeName(request.LastName);
        var maternalLastName = NormalizeOptionalName(request.MaternalLastName);

        var username = await _usuarioFacade.GenerateUsernameAsync(firstName, lastName, maternalLastName);
        var temporaryPassword = _usuarioFacade.GenerateTemporaryPassword();

        var keycloakIdentity = new UsuarioIdentidad(username, normalizedEmail, firstName, BuildLastNames(lastName, maternalLastName));
        var keycloakResult = await _usuarioFacade.RegisterUserInKeycloakAsync(keycloakIdentity, request.Role.Trim(), temporaryPassword);

        if (!keycloakResult.IsSuccess)
        {
            return Result<CreateUserResultDto>.Fallo(Result.Error(string.Join(" ", keycloakResult.Errors())));
        }

        return await PersistAndNotifyAsync(keycloakResult.Valor, firstName, lastName, maternalLastName, normalizedEmail, username, roleId, temporaryPassword);
    }

    private async Task<Result<CreateUserResultDto>> PersistAndNotifyAsync(
        Guid keycloakId,
        string firstName,
        string lastName,
        string? maternalLastName,
        string normalizedEmail,
        string username,
        int roleId,
        string temporaryPassword)
    {
        var user = new User
        {
            KeycloakId = keycloakId,
            FirstName = firstName,
            LastName = lastName,
            MaternalLastName = maternalLastName,
            Email = normalizedEmail,
            Username = username,
            FullName = BuildFullName(firstName, lastName, maternalLastName),
            RoleId = roleId,
            Status = UserStatuses.Pending,
            CreatedByUserId = _currentUser.UserId
        };

        try
        {
            var created = await _transactionRunner.ExecuteAsync(async () =>
            {
                var publicId = await _userRepository.CreatePendingAsync(user);
                await _usuarioFacade.SendTemporaryCredentialsEmailAsync(normalizedEmail, firstName, username, temporaryPassword);
                return new CreateUserResultDto(publicId, username);
            });

            return Result<CreateUserResultDto>.Exito(created);
        }
        catch (Exception)
        {
            await _usuarioFacade.CompensateKeycloakAsync(keycloakId);
            return Result<CreateUserResultDto>.Fallo(
                Result.Error("No fue posible enviar el correo con las credenciales temporales. Por favor, repita el proceso."));
        }
    }

    private static Result Validate(CreateUserRequest request)
    {
        var requiredError = ValidateRequiredFields(request);
        if (!requiredError.EsExito)
        {
            return requiredError;
        }

        var formatError = ValidateFieldFormats(request);
        if (!formatError.EsExito)
        {
            return formatError;
        }

        return Result.Exito();
    }

    private static Result ValidateRequiredFields(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return Result.Validacion(nameof(request.FirstName), "El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Validacion(nameof(request.LastName), "El apellido paterno es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Result.Validacion(nameof(request.Email), "El correo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return Result.Validacion(nameof(request.Role), "Debe seleccionar un rol válido.");
        }

        return Result.Exito();
    }

    private static Result ValidateFieldFormats(CreateUserRequest request)
    {
        if (!IsValidNameText(request.FirstName))
        {
            return Result.Validacion(nameof(request.FirstName), "El nombre solo permite letras, incluyendo acentos y ñ.");
        }

        if (!IsValidNameText(request.LastName))
        {
            return Result.Validacion(nameof(request.LastName), "El apellido solo permite letras, incluyendo acentos y ñ.");
        }

        if (!IsValidOptionalNameText(request.MaternalLastName))
        {
            return Result.Validacion(nameof(request.MaternalLastName), "El apellido materno solo permite letras, incluyendo acentos y ñ.");
        }

        return ValidateEmailAddress(request.Email);
    }

    private static bool IsValidNameText(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length > 0 && trimmed.All(character => char.IsLetter(character) || character == ' ');
    }

    private static bool IsValidOptionalNameText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return IsValidNameText(value);
    }

    private static Result ValidateEmailAddress(string email)
    {
        try
        {
            var address = new MailAddress(email.Trim());
            if (!string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return Result.Validacion("Email", "El correo no tiene un formato válido.");
            }

            return Result.Exito();
        }
        catch (FormatException)
        {
            return Result.Validacion("Email", "El correo no tiene un formato válido.");
        }
    }

    private static string NormalizeName(string value)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(CapitalizeWord));
    }

    private static string? NormalizeOptionalName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeName(value);
    }

    private static string CapitalizeWord(string word)
    {
        if (word.Length == 0)
        {
            return string.Empty;
        }

        return char.ToUpper(word[0]) + word[1..].ToLower();
    }

    private static string BuildLastNames(string lastName, string? maternalLastName)
    {
        if (string.IsNullOrWhiteSpace(maternalLastName))
        {
            return lastName;
        }

        return $"{lastName} {maternalLastName}";
    }

    private static string BuildFullName(string firstName, string lastName, string? maternalLastName)
    {
        if (string.IsNullOrWhiteSpace(maternalLastName))
        {
            return $"{firstName} {lastName}";
        }

        return $"{firstName} {lastName} {maternalLastName}";
    }
}
