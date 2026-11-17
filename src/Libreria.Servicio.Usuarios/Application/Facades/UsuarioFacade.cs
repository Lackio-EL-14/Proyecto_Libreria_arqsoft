using System.Net;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.Services;

namespace Libreria.Servicio.Usuarios.Application.Facades;

/// <summary>
/// Fachada de alta de usuarios. En US-124 solo se usa la generación del
/// nombre de usuario; la contraseña temporal y su envío quedan para US-130.
/// </summary>
public sealed class UsuarioFacade
{
    private readonly UsernameGenerator _usernameGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IApplicationConfiguration _configuration;

    public UsuarioFacade(
        UsernameGenerator usernameGenerator,
        IEmailSender emailSender,
        IApplicationConfiguration configuration)
    {
        _usernameGenerator = usernameGenerator;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public Task<string> GenerateUsernameAsync(
        string firstName,
        string paternalLastName) =>
        _usernameGenerator.GenerateAsync(firstName, paternalLastName);

    /// <summary>
    /// Punto de integración previsto para US-130. Nunca es invocado por
    /// CreateUserUseCase en US-124, que crea un usuario Pendiente sin correo.
    /// </summary>
    public Task SendTemporaryCredentialsEmailAsync(
        string recipient,
        string firstName,
        string username,
        string temporaryPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPassword);

        var loginUrl = BuildLoginUrl();
        var body = $"""
            <h2>Bienvenido al Sistema Librería</h2>
            <p>Hola {WebUtility.HtmlEncode(firstName)},</p>
            <p>Tu usuario es <strong>{WebUtility.HtmlEncode(username)}</strong>.</p>
            <p>Contraseña temporal: {WebUtility.HtmlEncode(temporaryPassword)}</p>
            <p><a href="{WebUtility.HtmlEncode(loginUrl)}">Iniciar sesión</a></p>
            <p>Al ingresar, cambia tu contraseña temporal.</p>
            """;

        return _emailSender.SendAsync(
            recipient,
            "Acceso temporal - Sistema Librería",
            body);
    }

    private string BuildLoginUrl()
    {
        var configuredBaseUrl = _configuration.BaseUrl.Trim();

        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps &&
             baseUri.Scheme != Uri.UriSchemeHttp) ||
            !string.IsNullOrEmpty(baseUri.UserInfo))
        {
            throw new InvalidOperationException(
                "BASE_URL debe ser una dirección HTTP o HTTPS válida del Front.");
        }

        return $"{baseUri.GetLeftPart(UriPartial.Path).TrimEnd('/')}/Cuenta/Login";
    }
}
