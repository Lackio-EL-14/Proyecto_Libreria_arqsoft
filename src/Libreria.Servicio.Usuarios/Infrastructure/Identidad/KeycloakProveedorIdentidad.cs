using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Domain;
using Microsoft.Extensions.Options;

namespace Libreria.Servicio.Usuarios.Infrastructure.Identidad;

/// <summary>
/// Adaptador de <see cref="IProveedorIdentidad"/> sobre la API de administración de Keycloak.
/// Se autentica con la cuenta de servicio <c>libreria-usuarios-admin</c> (solo
/// <c>manage-users</c> y <c>view-users</c>) y verifica contraseñas con
/// <c>libreria-usuarios-verificacion</c> (solo direct grant, token sin audiencia ni roles).
/// </summary>
public sealed class KeycloakProveedorIdentidad : IProveedorIdentidad
{
    private const string AccionCambioContrasena = "UPDATE_PASSWORD";

    private readonly HttpClient _http;
    private readonly KeycloakOpciones _opciones;
    private readonly CacheTokenKeycloak _cache;

    public KeycloakProveedorIdentidad(
        HttpClient http,
        IOptions<KeycloakOpciones> opciones,
        CacheTokenKeycloak cache)
    {
        _http = http;
        _opciones = opciones.Value;
        _cache = cache;
    }

    public async Task<Result<Guid>> CrearUsuarioAsync(UsuarioIdentidad usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var cuerpo = new
        {
            username = usuario.NombreUsuario,
            email = usuario.Correo,
            firstName = usuario.Nombres,
            lastName = usuario.Apellidos,
            enabled = true,
            emailVerified = true
        };

        using var respuesta = await EnviarAdminAsync(HttpMethod.Post, "/users", cuerpo, cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.Conflict)
        {
            return Result<Guid>.Fallo(Result.Conflicto(nameof(usuario.NombreUsuario), MensajesIdentidad.UsuarioDuplicado));
        }

        if (respuesta.StatusCode == HttpStatusCode.BadRequest)
        {
            return Result<Guid>.Error(await LeerMensajeAsync(respuesta, cancellationToken)
                ?? "Keycloak rechazó los datos del usuario.");
        }

        await AsegurarExitoAsync(respuesta, "crear el usuario", cancellationToken);

        var ubicacion = respuesta.Headers.Location?.ToString() ?? string.Empty;
        return Guid.TryParse(ubicacion[(ubicacion.LastIndexOf('/') + 1)..], out var id)
            ? Result<Guid>.Exito(id)
            : throw new InvalidOperationException("Keycloak no devolvió el identificador del usuario creado.");
    }

    public async Task<Result> AsignarRolAsync(Guid idUsuario, string rol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rol);

        // «available» solo necesita view-users; GET /roles/{nombre} exigiría view-realm.
        var disponibles = await ObtenerRolesAsync(idUsuario, "available", cancellationToken);
        if (disponibles is null)
        {
            return Result.NoEncontrado(MensajesIdentidad.UsuarioNoEncontrado);
        }

        var representacion = disponibles.FirstOrDefault(r => string.Equals(
            r?["name"]?.GetValue<string>(), rol, StringComparison.Ordinal));

        if (representacion is null)
        {
            var asignados = await ObtenerRolesAsync(idUsuario, string.Empty, cancellationToken);
            var yaLoTiene = asignados?.Any(r => string.Equals(
                r?["name"]?.GetValue<string>(), rol, StringComparison.Ordinal)) == true;

            return yaLoTiene ? Result.Exito() : Result.Validacion("Rol", MensajesIdentidad.RolInvalido);
        }

        using var respuesta = await EnviarAdminAsync(
            HttpMethod.Post,
            $"/users/{idUsuario}/role-mappings/realm",
            new JsonArray(representacion.DeepClone()),
            cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return Result.NoEncontrado(MensajesIdentidad.UsuarioNoEncontrado);
        }

        await AsegurarExitoAsync(respuesta, "asignar el rol", cancellationToken);
        return Result.Exito();
    }

    public async Task<Result> FijarContrasenaAsync(Guid idUsuario, string contrasena, bool temporal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contrasena);

        using var respuesta = await EnviarAdminAsync(
            HttpMethod.Put,
            $"/users/{idUsuario}/reset-password",
            new { type = "password", value = contrasena, temporary = temporal },
            cancellationToken);

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return Result.NoEncontrado(MensajesIdentidad.UsuarioNoEncontrado);
        }

        if (respuesta.StatusCode == HttpStatusCode.BadRequest)
        {
            var error = await LeerJsonAsync(respuesta, cancellationToken);
            return Result.Validacion("Contrasena", MensajesIdentidad.DePolitica(error?["error"]?.GetValue<string>()));
        }

        await AsegurarExitoAsync(respuesta, "fijar la contraseña", cancellationToken);
        return Result.Exito();
    }

    public async Task<Result> VerificarContrasenaAsync(string nombreUsuario, string contrasena, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreUsuario);
        if (string.IsNullOrEmpty(contrasena))
        {
            return Result.Validacion("ContrasenaActual", MensajesIdentidad.ContrasenaActualIncorrecta);
        }

        var secreto = Requerir(_opciones.VerificacionClientSecret, "Keycloak__VerificacionClientSecret");
        using var respuesta = await _http.PostAsync(_opciones.UrlToken, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = _opciones.VerificacionClientId,
            ["client_secret"] = secreto,
            ["username"] = nombreUsuario,
            ["password"] = contrasena
        }), cancellationToken);

        if (respuesta.IsSuccessStatusCode)
        {
            // Solo interesaba saber si la contraseña es correcta: se cierra la sesión creada.
            var tokens = await LeerJsonAsync(respuesta, cancellationToken);
            await CerrarSesionVerificacionAsync(tokens?["refresh_token"]?.GetValue<string>(), secreto, cancellationToken);
            return Result.Exito();
        }

        if (respuesta.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
        {
            var error = await LeerJsonAsync(respuesta, cancellationToken);
            if (error?["error"]?.GetValue<string>() == "invalid_grant")
            {
                return (error["error_description"]?.GetValue<string>()) switch
                {
                    "Account is not fully set up" => Result.Validacion("ContrasenaActual", MensajesIdentidad.CambioPendiente),
                    "Account disabled" => Result.Validacion("ContrasenaActual", MensajesIdentidad.CuentaDeshabilitada),
                    _ => Result.Validacion("ContrasenaActual", MensajesIdentidad.ContrasenaActualIncorrecta)
                };
            }
        }

        throw new InvalidOperationException(
            $"Keycloak respondió {(int)respuesta.StatusCode} al verificar la contraseña; revise el cliente {_opciones.VerificacionClientId}.");
    }

    public Task<Result> HabilitarAsync(Guid idUsuario, CancellationToken cancellationToken = default) =>
        CambiarHabilitadoAsync(idUsuario, true, cancellationToken);

    public async Task<Result> DeshabilitarAsync(Guid idUsuario, CancellationToken cancellationToken = default)
    {
        var resultado = await CambiarHabilitadoAsync(idUsuario, false, cancellationToken);
        if (resultado.IsError)
        {
            return resultado;
        }

        using var respuesta = await EnviarAdminAsync(HttpMethod.Post, $"/users/{idUsuario}/logout", null, cancellationToken);
        await AsegurarExitoAsync(respuesta, "cerrar las sesiones del usuario", cancellationToken);
        return Result.Exito();
    }

    public async Task<Result<bool>> TieneCambioDeContrasenaPendienteAsync(Guid idUsuario, CancellationToken cancellationToken = default)
    {
        var usuario = await ObtenerUsuarioAsync(idUsuario, cancellationToken);
        if (usuario is null)
        {
            return Result<bool>.Fallo(Result.NoEncontrado(MensajesIdentidad.UsuarioNoEncontrado));
        }

        var pendiente = usuario["requiredActions"]?.AsArray()
            .Any(accion => accion?.GetValue<string>() == AccionCambioContrasena) == true;

        return Result<bool>.Exito(pendiente);
    }

    public async Task<Result> EliminarUsuarioAsync(Guid idUsuario, CancellationToken cancellationToken = default)
    {
        using var respuesta = await EnviarAdminAsync(HttpMethod.Delete, $"/users/{idUsuario}", null, cancellationToken);
        if (respuesta.StatusCode != HttpStatusCode.NotFound)
        {
            await AsegurarExitoAsync(respuesta, "eliminar el usuario", cancellationToken);
        }

        return Result.Exito();
    }

    private async Task<Result> CambiarHabilitadoAsync(Guid idUsuario, bool habilitado, CancellationToken cancellationToken)
    {
        // Se envía la representación completa: un PUT parcial puede borrar atributos del perfil.
        var usuario = await ObtenerUsuarioAsync(idUsuario, cancellationToken);
        if (usuario is null)
        {
            return Result.NoEncontrado(MensajesIdentidad.UsuarioNoEncontrado);
        }

        usuario["enabled"] = habilitado;
        using var respuesta = await EnviarAdminAsync(HttpMethod.Put, $"/users/{idUsuario}", usuario, cancellationToken);
        await AsegurarExitoAsync(respuesta, habilitado ? "habilitar el usuario" : "deshabilitar el usuario", cancellationToken);
        return Result.Exito();
    }

    private async Task<JsonObject?> ObtenerUsuarioAsync(Guid idUsuario, CancellationToken cancellationToken)
    {
        using var respuesta = await EnviarAdminAsync(HttpMethod.Get, $"/users/{idUsuario}", null, cancellationToken);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await AsegurarExitoAsync(respuesta, "consultar el usuario", cancellationToken);
        return (await LeerJsonAsync(respuesta, cancellationToken))?.AsObject();
    }

    private async Task<JsonArray?> ObtenerRolesAsync(Guid idUsuario, string sufijo, CancellationToken cancellationToken)
    {
        var ruta = $"/users/{idUsuario}/role-mappings/realm" + (sufijo.Length > 0 ? "/" + sufijo : string.Empty);
        using var respuesta = await EnviarAdminAsync(HttpMethod.Get, ruta, null, cancellationToken);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        await AsegurarExitoAsync(respuesta, "consultar los roles", cancellationToken);
        return (await LeerJsonAsync(respuesta, cancellationToken))?.AsArray() ?? [];
    }

    /// <summary>Llama a la API de administración; si el token guardado ya no sirve (401), pide otro y reintenta una vez.</summary>
    private async Task<HttpResponseMessage> EnviarAdminAsync(HttpMethod metodo, string ruta, object? cuerpo, CancellationToken cancellationToken)
    {
        for (var intento = 0; ; intento++)
        {
            var token = await _cache.ObtenerAsync(PedirTokenServicioAsync, cancellationToken);
            using var solicitud = new HttpRequestMessage(metodo, _opciones.UrlAdministracion + ruta);
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (cuerpo is not null)
            {
                solicitud.Content = JsonContent.Create(cuerpo, cuerpo.GetType());
            }

            var respuesta = await _http.SendAsync(solicitud, cancellationToken);
            if (respuesta.StatusCode != HttpStatusCode.Unauthorized || intento > 0)
            {
                return respuesta;
            }

            respuesta.Dispose();
            _cache.Invalidar();
        }
    }

    private async Task<(string Token, int ExpiraEnSegundos)> PedirTokenServicioAsync(CancellationToken cancellationToken)
    {
        var secreto = Requerir(_opciones.ClientSecret, "Keycloak__ClientSecret");
        using var respuesta = await _http.PostAsync(_opciones.UrlToken, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _opciones.AdminClientId,
            ["client_secret"] = secreto
        }), cancellationToken);

        if (!respuesta.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Keycloak rechazó la cuenta de servicio {_opciones.AdminClientId} ({(int)respuesta.StatusCode}); revise Keycloak__ClientSecret.");
        }

        var json = await LeerJsonAsync(respuesta, cancellationToken);
        var token = json?["access_token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Keycloak no devolvió el token de la cuenta de servicio.");
        return (token, json?["expires_in"]?.GetValue<int>() ?? 60);
    }

    private async Task CerrarSesionVerificacionAsync(string? refreshToken, string secreto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        try
        {
            using var _ = await _http.PostAsync(_opciones.UrlCierreSesion, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _opciones.VerificacionClientId,
                ["client_secret"] = secreto,
                ["refresh_token"] = refreshToken
            }), cancellationToken);
        }
        catch (HttpRequestException)
        {
            // La verificación ya se hizo; la sesión de verificación vence sola.
        }
    }

    private static async Task AsegurarExitoAsync(HttpResponseMessage respuesta, string accion, CancellationToken cancellationToken)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return;
        }

        var detalle = await LeerMensajeAsync(respuesta, cancellationToken);
        throw new InvalidOperationException(
            $"Keycloak respondió {(int)respuesta.StatusCode} al {accion}{(detalle is null ? "." : $": {detalle}")}");
    }

    private static async Task<string?> LeerMensajeAsync(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        var json = await LeerJsonAsync(respuesta, cancellationToken);
        return json?["errorMessage"]?.GetValue<string>()
            ?? json?["error_description"]?.GetValue<string>()
            ?? json?["error"]?.GetValue<string>();
    }

    private static async Task<JsonNode?> LeerJsonAsync(HttpResponseMessage respuesta, CancellationToken cancellationToken)
    {
        var texto = await respuesta.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(texto);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Requerir(string? valor, string variable) =>
        string.IsNullOrWhiteSpace(valor)
            ? throw new InvalidOperationException($"Falta la variable de entorno {variable} (ver .env.example).")
            : valor;
}
