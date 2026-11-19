using System.Net;
using System.Text;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Domain;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Microsoft.Extensions.Options;

namespace Libreria.Servicio.Usuarios.Tests.Identidad;

/// <summary>Adaptador de Keycloak contra respuestas grabadas de Keycloak 26.8 (sin red).</summary>
public sealed class KeycloakProveedorIdentidadTests
{
    private const string Admin = "http://kc.test/admin/realms/libreria";
    private const string Token = "http://kc.test/realms/libreria/protocol/openid-connect/token";
    private static readonly Guid Id = Guid.Parse("04780a13-06b0-4ca5-bf94-692d45b14f21");

    [Fact]
    public async Task CrearUsuario_LeeElIdDeLocation_YUsaLaCuentaDeServicio()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Post, $"{Admin}/users", _ => Respuesta(HttpStatusCode.Created, location: $"{Admin}/users/{Id}"));
        var resultado = await Crear(http).CrearUsuarioAsync(new UsuarioIdentidad("jperezr", "j@libreria.local", "Juan", "Pérez"));

        Assert.Equal(Id, resultado.Valor);
        var token = Assert.Single(http.Solicitudes, s => s.Url == Token);
        Assert.Contains("grant_type=client_credentials", token.Cuerpo);
        Assert.Contains("client_id=libreria-usuarios-admin", token.Cuerpo);
        Assert.Equal("Bearer token-servicio", http.Solicitudes.Last().Autorizacion);
    }

    [Fact]
    public async Task CrearUsuario_Duplicado_DevuelveConflicto()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Post, $"{Admin}/users", _ => Respuesta(HttpStatusCode.Conflict, """{"errorMessage":"User exists with same username"}"""));

        var resultado = await Crear(http).CrearUsuarioAsync(new UsuarioIdentidad("admin", "a@libreria.local", "A", "B"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal(MensajesIdentidad.UsuarioDuplicado, Assert.Single(resultado.Errors()));
    }

    [Theory]
    [InlineData("invalidPasswordMinLengthMessage", "La contraseña debe tener al menos 8 caracteres.")]
    [InlineData("invalidPasswordMinSpecialCharsMessage", "La contraseña debe contener al menos un carácter especial.")]
    [InlineData("invalidPasswordNotUsernameMessage", "La contraseña no puede ser igual al nombre de usuario.")]
    [InlineData("claveDesconocida", "La contraseña no cumple la política de seguridad.")]
    public async Task FijarContrasena_RechazadaPorPolitica_TraduceElMensaje(string clave, string esperado)
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Put, $"{Admin}/users/{Id}/reset-password",
                _ => Respuesta(HttpStatusCode.BadRequest, $$"""{"error":"{{clave}}","error_description":"Contraseña incorrecta: debe contener al menos 1 caracteres especiales."}"""));

        var resultado = await Crear(http).FijarContrasenaAsync(Id, "x", temporal: true);

        Assert.Equal(esperado, Assert.Single(resultado.Errors()));
    }

    [Fact]
    public async Task FijarContrasena_EnviaTemporal()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Put, $"{Admin}/users/{Id}/reset-password", _ => Respuesta(HttpStatusCode.NoContent));

        Assert.True((await Crear(http).FijarContrasenaAsync(Id, "Temporal#2026", temporal: true)).IsSuccess);
        Assert.Contains("\"temporary\":true", http.Solicitudes.Last().Cuerpo);
    }

    [Theory]
    [InlineData("Invalid user credentials", MensajesIdentidad.ContrasenaActualIncorrecta)]
    [InlineData("Account is not fully set up", MensajesIdentidad.CambioPendiente)]
    [InlineData("Account disabled", MensajesIdentidad.CuentaDeshabilitada)]
    public async Task VerificarContrasena_InvalidGrant_DevuelveMensaje(string descripcion, string esperado)
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Post, Token, s => s.Cuerpo.Contains("grant_type=password")
                ? Respuesta(HttpStatusCode.BadRequest, $$"""{"error":"invalid_grant","error_description":"{{descripcion}}"}""")
                : null);

        var resultado = await Crear(http).VerificarContrasenaAsync("jperezr", "Clave#2026");

        Assert.Equal(esperado, Assert.Single(resultado.Errors()));
        Assert.Contains("client_id=libreria-usuarios-verificacion", http.Solicitudes.Single().Cuerpo);
    }

    [Fact]
    public async Task VerificarContrasena_Correcta_CierraLaSesionDeVerificacion()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Post, Token, s => s.Cuerpo.Contains("grant_type=password")
                ? Respuesta(HttpStatusCode.OK, """{"access_token":"a","refresh_token":"r","expires_in":300}""")
                : null)
            .Responder(HttpMethod.Post, "http://kc.test/realms/libreria/protocol/openid-connect/logout", _ => Respuesta(HttpStatusCode.NoContent));

        Assert.True((await Crear(http).VerificarContrasenaAsync("jperezr", "Clave#2026")).IsSuccess);
        Assert.Contains(http.Solicitudes, s => s.Url.EndsWith("/logout") && s.Cuerpo.Contains("refresh_token=r"));
    }

    [Fact]
    public async Task TieneCambioPendiente_LeeRequiredActions()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Get, $"{Admin}/users/{Id}", _ => Respuesta(HttpStatusCode.OK, """{"id":"x","requiredActions":["UPDATE_PASSWORD"]}"""));

        Assert.True((await Crear(http).TieneCambioDeContrasenaPendienteAsync(Id)).Valor);
    }

    [Fact]
    public async Task Deshabilitar_EnviaLaRepresentacionCompleta_YCierraSesiones()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Get, $"{Admin}/users/{Id}", _ => Respuesta(HttpStatusCode.OK, """{"id":"x","username":"jperezr","email":"j@libreria.local","enabled":true}"""))
            .Responder(HttpMethod.Put, $"{Admin}/users/{Id}", _ => Respuesta(HttpStatusCode.NoContent))
            .Responder(HttpMethod.Post, $"{Admin}/users/{Id}/logout", _ => Respuesta(HttpStatusCode.NoContent));

        Assert.True((await Crear(http).DeshabilitarAsync(Id)).IsSuccess);

        var put = http.Solicitudes.Single(s => s.Metodo == HttpMethod.Put);
        Assert.Contains("\"enabled\":false", put.Cuerpo);
        Assert.Contains("\"email\":\"j@libreria.local\"", put.Cuerpo);
        Assert.Contains(http.Solicitudes, s => s.Url.EndsWith("/logout"));
    }

    [Fact]
    public async Task UsuarioInexistente_DevuelveNoEncontrado_YEliminarEsIdempotente()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Get, $"{Admin}/users/{Id}", _ => Respuesta(HttpStatusCode.NotFound))
            .Responder(HttpMethod.Delete, $"{Admin}/users/{Id}", _ => Respuesta(HttpStatusCode.NotFound));
        var proveedor = Crear(http);

        Assert.Equal(TipoError.NoEncontrado, (await proveedor.HabilitarAsync(Id)).Tipo);
        Assert.True((await proveedor.EliminarUsuarioAsync(Id)).IsSuccess);
    }

    [Fact]
    public async Task AsignarRol_UsaRolesDisponibles_SinView_Realm()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Get, $"{Admin}/users/{Id}/role-mappings/realm/available",
                _ => Respuesta(HttpStatusCode.OK, """[{"id":"r1","name":"Administrador"},{"id":"r2","name":"Vendedor"}]"""))
            .Responder(HttpMethod.Post, $"{Admin}/users/{Id}/role-mappings/realm", _ => Respuesta(HttpStatusCode.NoContent));

        Assert.True((await Crear(http).AsignarRolAsync(Id, "Vendedor")).IsSuccess);
        Assert.Contains("\"name\":\"Vendedor\"", http.Solicitudes.Last().Cuerpo);
    }

    [Fact]
    public async Task TokenRechazado_SePideOtro_YSeReintentaUnaVez()
    {
        var llamadas = 0;
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Get, $"{Admin}/users/{Id}", _ => ++llamadas == 1
                ? Respuesta(HttpStatusCode.Unauthorized)
                : Respuesta(HttpStatusCode.OK, """{"requiredActions":[]}"""));

        Assert.False((await Crear(http).TieneCambioDeContrasenaPendienteAsync(Id)).Valor);
        Assert.Equal(2, http.Solicitudes.Count(s => s.Url == Token));
    }

    [Fact]
    public async Task KeycloakCaido_LanzaExcepcion_NoResult()
    {
        var http = new ManejadorFalso()
            .Responder(HttpMethod.Get, $"{Admin}/users/{Id}", _ => Respuesta(HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Crear(http).TieneCambioDeContrasenaPendienteAsync(Id));
    }

    [Fact]
    public async Task SinSecreto_LanzaExcepcionQueNombraLaVariable()
    {
        var proveedor = new KeycloakProveedorIdentidad(new HttpClient(new ManejadorFalso()),
            Options.Create(new KeycloakOpciones { Authority = "http://kc.test/realms/libreria" }),
            new CacheTokenKeycloak(TimeProvider.System));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => proveedor.EliminarUsuarioAsync(Id));
        Assert.Contains("Keycloak__ClientSecret", error.Message);
    }

    private static KeycloakProveedorIdentidad Crear(ManejadorFalso http) =>
        new(new HttpClient(http), Options.Create(new KeycloakOpciones
        {
            Authority = "http://kc.test/realms/libreria/",
            ClientSecret = "secreto-falso",
            VerificacionClientSecret = "secreto-falso-2"
        }), new CacheTokenKeycloak(TimeProvider.System));

    private static HttpResponseMessage Respuesta(HttpStatusCode estado, string? json = null, string? location = null)
    {
        var respuesta = new HttpResponseMessage(estado);
        if (json is not null)
        {
            respuesta.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        if (location is not null)
        {
            respuesta.Headers.Location = new Uri(location);
        }

        return respuesta;
    }

    private sealed record Solicitud(HttpMethod Metodo, string Url, string Cuerpo, string? Autorizacion);

    /// <summary>Responde según método y URL; el token de la cuenta de servicio se responde solo.</summary>
    private sealed class ManejadorFalso : HttpMessageHandler
    {
        private readonly List<(HttpMethod Metodo, string Url, Func<Solicitud, HttpResponseMessage?> Responder)> _rutas = [];

        public List<Solicitud> Solicitudes { get; } = [];

        public ManejadorFalso Responder(HttpMethod metodo, string url, Func<Solicitud, HttpResponseMessage?> responder)
        {
            _rutas.Add((metodo, url, responder));
            return this;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cuerpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            var solicitud = new Solicitud(request.Method, request.RequestUri!.ToString(), cuerpo, request.Headers.Authorization?.ToString());
            Solicitudes.Add(solicitud);

            foreach (var ruta in _rutas.Where(r => r.Metodo == request.Method && r.Url == solicitud.Url))
            {
                if (ruta.Responder(solicitud) is { } respuesta)
                {
                    return respuesta;
                }
            }

            if (solicitud.Url == Token && cuerpo.Contains("grant_type=client_credentials"))
            {
                return Respuesta(HttpStatusCode.OK, """{"access_token":"token-servicio","expires_in":300}""");
            }

            return Respuesta(HttpStatusCode.NotImplemented);
        }
    }
}
