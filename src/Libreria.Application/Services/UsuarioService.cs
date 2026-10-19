using System.Data;
using System.Net;
using System.Net.Mail;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;
using Libreria.Application.Validators;

namespace Libreria.Application.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IGeneradorNombreUsuario _generadorNombreUsuario;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguracionAplicacion _configuracion;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IContrasenaHasher _hasher;
    private readonly PasswordPolicyValidator _passwordPolicy;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IGeneradorNombreUsuario generadorNombreUsuario,
        IEmailSender emailSender,
        IConfiguracionAplicacion configuracion,
        IDbConnectionFactory connectionFactory,
        IUsuarioActual usuarioActual,
        IContrasenaHasher hasher,
        PasswordPolicyValidator passwordPolicy)
    {
        _usuarioRepository = usuarioRepository;
        _generadorNombreUsuario = generadorNombreUsuario;
        _emailSender = emailSender;
        _configuracion = configuracion;
        _connectionFactory = connectionFactory;
        _usuarioActual = usuarioActual;
        _hasher = hasher;
        _passwordPolicy = passwordPolicy;
    }

    public async Task<ResultadoOperacion> CambiarContrasenaAsync(CambioContrasenaInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var usuarioId = _usuarioActual.UsuarioId;
        var errores = new Dictionary<string, string>();

        if (usuarioId is not > 0)
            return ResultadoOperacion.Invalido(new Dictionary<string, string>
                { [string.Empty] = "Debe iniciar sesión para cambiar su contraseña." });

        if (string.IsNullOrEmpty(input.ContrasenaActual))
            errores["Input.ContrasenaActual"] = "Ingrese su contraseña actual.";
        if (!string.Equals(input.NuevaContrasena, input.Confirmacion, StringComparison.Ordinal))
            errores["Input.Confirmacion"] = "La confirmación no coincide con la nueva contraseña.";

        var politica = _passwordPolicy.Validar(input.NuevaContrasena);
        if (politica.Count > 0)
            errores["Input.NuevaContrasena"] = string.Join(" ", politica);
        else if (input.NuevaContrasena.Length > 128)
            errores["Input.NuevaContrasena"] = "La contraseña no puede superar los 128 caracteres.";

        if (errores.Count > 0)
            return ResultadoOperacion.Invalido(errores);

        var hashActual = await _usuarioRepository.ObtenerHashActivoAsync(usuarioId.Value);
        if (hashActual is null || !_hasher.Verificar(hashActual, input.ContrasenaActual))
            return ResultadoOperacion.Invalido(new Dictionary<string, string>
                { ["Input.ContrasenaActual"] = "La contraseña actual es incorrecta." });

        if (string.Equals(input.NuevaContrasena, input.ContrasenaActual, StringComparison.Ordinal))
            return ResultadoOperacion.Invalido(new Dictionary<string, string>
                { ["Input.NuevaContrasena"] = "La nueva contraseña debe ser distinta de la actual." });

        var actualizado = await _usuarioRepository.ActualizarContrasenaAsync(
            usuarioId.Value, hashActual, _hasher.Hashear(input.NuevaContrasena));
        return actualizado
            ? ResultadoOperacion.Correcto()
            : ResultadoOperacion.Invalido(new Dictionary<string, string>
                { [string.Empty] = "No fue posible cambiar la contraseña. Intente nuevamente." });
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
