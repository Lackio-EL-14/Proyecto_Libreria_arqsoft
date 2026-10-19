using System.Data;
using System.Net;
using System.Net.Mail;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;

namespace Libreria.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IGeneradorNombreUsuario _generadorNombreUsuario;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguracionAplicacion _configuracion;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUsuarioActual _usuarioActual;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IGeneradorNombreUsuario generadorNombreUsuario,
        IEmailSender emailSender,
        IConfiguracionAplicacion configuracion,
        IDbConnectionFactory connectionFactory,
        IUsuarioActual usuarioActual)
    {
        _usuarioRepository = usuarioRepository;
        _generadorNombreUsuario = generadorNombreUsuario;
        _emailSender = emailSender;
        _configuracion = configuracion;
        _connectionFactory = connectionFactory;
        _usuarioActual = usuarioActual;
    }

    public async Task<string> CrearPendienteAsync(UsuarioAltaInput input)
    {
        ValidarInput(input);

        var correo = input.Correo.Trim();

        if (await _usuarioRepository.ExisteCorreoAsync(correo))
        {
            throw new ArgumentException(
                "Ya existe un usuario registrado con ese correo.",
                nameof(input));
        }

        var usuarioCreacionId = _usuarioActual.UsuarioId
            ?? throw new InvalidOperationException(
                "No existe un usuario autenticado.");

        var nombreUsuario =
            await _generadorNombreUsuario.GenerarAsync(
                input.Nombre,
                input.Apellido);

        var token = TokenActivacionService.GenerarToken();
        var tokenHash = TokenActivacionService.CalcularHash(token);
        var expiracion = DateTime.UtcNow.AddHours(24);

        var usuario = new Usuario
        {
            Nombre = input.Nombre.Trim(),
            Apellido = input.Apellido.Trim(),
            Correo = correo,
            NombreUsuario = nombreUsuario,
            NombreCompleto =
                $"{input.Nombre.Trim()} {input.Apellido.Trim()}",
            PasswordHash = string.Empty,
            RolId = input.RolId,
            Estado = EstadosUsuario.Pendiente,
            TokenActivacionHash = tokenHash,
            TokenActivacionExpiracion = expiracion,
            TokenActivacionFechaUso = null,
            UsuarioCreacionId = usuarioCreacionId
        };

        await using var connection = _connectionFactory.CreateConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await _usuarioRepository.InsertarAsync(
                usuario,
                connection,
                transaction);

            var enlace = ConstruirEnlaceActivacion(
                tokenHash,
                token);

            var cuerpoHtml = ConstruirCorreo(
                usuario.Nombre,
                usuario.NombreUsuario,
                enlace);

            await _emailSender.EnviarAsync(
                usuario.Correo,
                "Activa tu cuenta - Sistema Librería",
                cuerpoHtml);

            await transaction.CommitAsync();

            return nombreUsuario;
        }
        catch (Exception error)
        {
            await transaction.RollbackAsync();

            throw new InvalidOperationException(
                "No fue posible completar el alta del usuario.",
                error);
        }
    }

    private static void ValidarInput(UsuarioAltaInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Nombre))
        {
            throw new ArgumentException(
                "El nombre es obligatorio.",
                nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Apellido))
        {
            throw new ArgumentException(
                "El apellido es obligatorio.",
                nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Correo))
        {
            throw new ArgumentException(
                "El correo es obligatorio.",
                nameof(input));
        }

        try
        {
            _ = new MailAddress(input.Correo.Trim());
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                "El correo no tiene un formato válido.",
                nameof(input));
        }

        if (input.RolId <= 0)
        {
            throw new ArgumentException(
                "Debe seleccionar un rol válido.",
                nameof(input));
        }
    }

    private string ConstruirEnlaceActivacion(
        string tokenHash,
        string token)
    {
        var hashCodificado = WebUtility.UrlEncode(tokenHash);
        var tokenCodificado = WebUtility.UrlEncode(token);

        return
            $"{_configuracion.BaseUrl}/Usuarios/Activar" +
            $"?h={hashCodificado}&t={tokenCodificado}";
    }

    private static string ConstruirCorreo(
        string nombre,
        string nombreUsuario,
        string enlace)
    {
        return $"""
            <h2>Activa tu cuenta</h2>
            <p>Hola {WebUtility.HtmlEncode(nombre)},</p>
            <p>Tu nombre de usuario es:
               <strong>{WebUtility.HtmlEncode(nombreUsuario)}</strong>
            </p>
            <p>Para definir tu contraseña y activar tu cuenta,
               utiliza el siguiente enlace:</p>
            <p>
                <a href="{WebUtility.HtmlEncode(enlace)}">
                    Activar cuenta
                </a>
            </p>
            <p>Este enlace tiene una vigencia de 24 horas.</p>
            """;
    }
}