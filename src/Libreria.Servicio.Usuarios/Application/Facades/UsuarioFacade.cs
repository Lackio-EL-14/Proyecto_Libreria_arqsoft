using System.Net;
using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Application.Services;

namespace Libreria.Servicio.Usuarios.Application.Facades;

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

    public async Task<UserActivationPreparation>
        PrepareActivationAsync(
            string firstName,
            string lastName)
    {
        var username =
            await _usernameGenerator.GenerateAsync(
                firstName,
                lastName);

        var token =
            ActivationTokenService.Generate();

        var tokenHash =
            ActivationTokenService.CalculateHash(token);

        return new UserActivationPreparation(
            username,
            token,
            tokenHash,
            DateTime.UtcNow.AddHours(24));
    }

    public Task SendActivationEmailAsync(
        string recipient,
        string firstName,
        UserActivationPreparation activation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentNullException.ThrowIfNull(activation);

        var link = BuildActivationLink(
            activation.TokenHash,
            activation.Token);

        var body = $"""
            <h2>Activa tu cuenta</h2>
            <p>Hola {WebUtility.HtmlEncode(firstName)},</p>
            <p>
                Tu nombre de usuario es
                <strong>{WebUtility.HtmlEncode(activation.Username)}</strong>.
            </p>
            <p>
                Para definir tu contraseña y activar tu cuenta,
                utiliza el siguiente enlace:
            </p>
            <p>
                <a href="{WebUtility.HtmlEncode(link)}">
                    Activar cuenta
                </a>
            </p>
            <p>Este enlace tiene una vigencia de 24 horas.</p>
            """;

        return _emailSender.SendAsync(
            recipient,
            "Activa tu cuenta - Sistema Librería",
            body);
    }

    private string BuildActivationLink(
        string tokenHash,
        string token)
    {
        var baseUrl =
            _configuration.BaseUrl.TrimEnd('/');

        return
            $"{baseUrl}/Usuarios/Activar" +
            $"?h={WebUtility.UrlEncode(tokenHash)}" +
            $"&t={WebUtility.UrlEncode(token)}";
    }
}