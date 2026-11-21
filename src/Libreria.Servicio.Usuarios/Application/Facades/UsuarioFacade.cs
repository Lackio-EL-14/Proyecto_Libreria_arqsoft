using System.Net;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.Services;

namespace Libreria.Servicio.Usuarios.Application.Facades;

public sealed class UsuarioFacade
{
    private readonly UsernameGenerator _usernameGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IApplicationConfiguration _configuration;
    private readonly IProveedorIdentidad? _proveedorIdentidad;
    private readonly TemporaryPasswordGenerator _passwordGenerator;

    public UsuarioFacade(
        UsernameGenerator usernameGenerator,
        IEmailSender emailSender,
        IApplicationConfiguration configuration,
        IProveedorIdentidad? proveedorIdentidad = null,
        TemporaryPasswordGenerator? passwordGenerator = null)
    {
        _usernameGenerator = usernameGenerator;
        _emailSender = emailSender;
        _configuration = configuration;
        _proveedorIdentidad = proveedorIdentidad;
        _passwordGenerator = passwordGenerator ?? new TemporaryPasswordGenerator();
    }

    public Task<string> GenerateUsernameAsync(
        string firstName,
        string paternalLastName,
        string? maternalLastName = null) =>
        _usernameGenerator.GenerateAsync(firstName, paternalLastName, maternalLastName);

    public string GenerateTemporaryPassword() =>
        _passwordGenerator.Generate();

    public async Task<Domain.Result<Guid>> RegisterUserInKeycloakAsync(
        UsuarioIdentidad identidad,
        string role,
        string temporaryPassword,
        CancellationToken cancellationToken = default)
    {
        if (_proveedorIdentidad is null)
        {
            return Domain.Result<Guid>.Error("El proveedor de identidad no está configurado.");
        }

        var createResult = await _proveedorIdentidad.CrearUsuarioAsync(identidad, cancellationToken);
        if (!createResult.IsSuccess)
        {
            return createResult;
        }

        var keycloakId = createResult.Valor;
        return await CompleteKeycloakSetupAsync(keycloakId, role, temporaryPassword, cancellationToken);
    }

    private async Task<Domain.Result<Guid>> CompleteKeycloakSetupAsync(
        Guid keycloakId,
        string role,
        string temporaryPassword,
        CancellationToken cancellationToken)
    {
        var roleResult = await _proveedorIdentidad!.AsignarRolAsync(keycloakId, role, cancellationToken);
        if (!roleResult.IsSuccess)
        {
            await CompensateKeycloakAsync(keycloakId, cancellationToken);
            return Domain.Result<Guid>.Error(string.Join(" ", roleResult.Errors()));
        }

        var passResult = await _proveedorIdentidad.FijarContrasenaAsync(keycloakId, temporaryPassword, true, cancellationToken);
        if (!passResult.IsSuccess)
        {
            await CompensateKeycloakAsync(keycloakId, cancellationToken);
            return Domain.Result<Guid>.Error(string.Join(" ", passResult.Errors()));
        }

        return Domain.Result<Guid>.Success(keycloakId);
    }

    public async Task CompensateKeycloakAsync(Guid keycloakId, CancellationToken cancellationToken = default)
    {
        if (_proveedorIdentidad is not null)
        {
            await _proveedorIdentidad.EliminarUsuarioAsync(keycloakId, cancellationToken);
        }
    }

    public Task SendTemporaryCredentialsEmailAsync(
        string recipient,
        string firstName,
        string username,
        string temporaryPassword)
    {
        ValidateEmailParameters(recipient, firstName, username, temporaryPassword);

        var loginUrl = BuildLoginUrl();
        var body = BuildEmailBody(firstName, username, temporaryPassword, loginUrl);

        return _emailSender.SendAsync(
            recipient,
            "Acceso temporal - Sistema Librería",
            body);
    }

    private static void ValidateEmailParameters(
        string recipient,
        string firstName,
        string username,
        string temporaryPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPassword);
    }

    private static string BuildEmailBody(
        string firstName,
        string username,
        string temporaryPassword,
        string loginUrl)
    {
        return $"""
            <h2>Le damos la bienvenida al Sistema Librería</h2>
            <p>Hola {WebUtility.HtmlEncode(firstName)},</p>
            <p>Su usuario es <strong>{WebUtility.HtmlEncode(username)}</strong>.</p>
            <p>Contraseña temporal: {WebUtility.HtmlEncode(temporaryPassword)}</p>
            <p><a href="{WebUtility.HtmlEncode(loginUrl)}">Iniciar sesión</a></p>
            <p>Al ingresar, el sistema le pedirá cambiar esta contraseña temporal.</p>
            """;
    }

    private string BuildLoginUrl()
    {
        var baseUri = ValidateBaseUrl(_configuration.BaseUrl);
        // El Front reconoce este motivo: si en el navegador hay otra sesión abierta, pide cerrarla
        // antes de ingresar con la cuenta nueva (US-131).
        return $"{baseUri.GetLeftPart(UriPartial.Path).TrimEnd('/')}/Cuenta/Login?motivo=primer-ingreso";
    }

    private static Uri ValidateBaseUrl(string rawUrl)
    {
        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var baseUri))
        {
            throw new InvalidOperationException(
                "BASE_URL debe ser una dirección HTTP o HTTPS válida del Front.");
        }

        EnsureValidSchemeAndUserInfo(baseUri);
        return baseUri;
    }

    private static void EnsureValidSchemeAndUserInfo(Uri baseUri)
    {
        var isHttp = baseUri.Scheme == Uri.UriSchemeHttps || baseUri.Scheme == Uri.UriSchemeHttp;

        if (!isHttp || !string.IsNullOrEmpty(baseUri.UserInfo))
        {
            throw new InvalidOperationException(
                "BASE_URL debe ser una dirección HTTP o HTTPS válida del Front.");
        }
    }
}
