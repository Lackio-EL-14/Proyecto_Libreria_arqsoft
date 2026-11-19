using System.Net;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Infrastructure.Identidad;
using Microsoft.Extensions.Options;

namespace Libreria.Servicio.Usuarios.Tests.Identidad;

/// <summary>Se ejecuta solo con <c>KEYCLOAK_PRUEBAS_INTEGRACION=1</c>; si no, la prueba aparece como omitida.</summary>
public sealed class KeycloakFactAttribute : FactAttribute
{
    public KeycloakFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("KEYCLOAK_PRUEBAS_INTEGRACION") != "1")
        {
            Skip = "Requiere Keycloak levantado (docker compose up -d) y KEYCLOAK_PRUEBAS_INTEGRACION=1 " +
                   "con Keycloak__ClientSecret y Keycloak__VerificacionClientSecret en el entorno.";
        }
    }
}

/// <summary>
/// Recorre el puerto completo contra el Keycloak de docker compose (US-124, evidencia de la defensa).
/// Crea un usuario de prueba y lo borra al final.
/// </summary>
public sealed class KeycloakIntegracionTests
{
    private const string Authority = "http://localhost:8080/realms/libreria";

    [KeycloakFact]
    public async Task CicloCompleto_ContrasenaTemporalNoEmiteToken_HastaCambiarla()
    {
        var proveedor = new KeycloakProveedorIdentidad(new HttpClient(), Options.Create(new KeycloakOpciones
        {
            Authority = Authority,
            ClientSecret = Environment.GetEnvironmentVariable("Keycloak__ClientSecret"),
            VerificacionClientSecret = Environment.GetEnvironmentVariable("Keycloak__VerificacionClientSecret")
        }), new CacheTokenKeycloak(TimeProvider.System));

        var usuario = $"prueba.{Guid.NewGuid():N}"[..20];
        var id = (await proveedor.CrearUsuarioAsync(new UsuarioIdentidad(usuario, $"{usuario}@libreria.local", "Prueba", "Integración"))).Valor;
        try
        {
            Assert.True((await proveedor.CrearUsuarioAsync(new UsuarioIdentidad(usuario, "otro@libreria.local", "X", "Y"))).IsError);
            Assert.True((await proveedor.AsignarRolAsync(id, "Vendedor")).IsSuccess);
            Assert.True((await proveedor.AsignarRolAsync(id, "Vendedor")).IsSuccess);
            Assert.True((await proveedor.AsignarRolAsync(id, "NoExiste")).IsError);

            Assert.Equal("La contraseña debe contener al menos un carácter especial.",
                Assert.Single((await proveedor.FijarContrasenaAsync(id, "SinEspecial123", temporal: true)).Errors()));

            Assert.True((await proveedor.FijarContrasenaAsync(id, "Temporal#2026x", temporal: true)).IsSuccess);
            Assert.True((await proveedor.TieneCambioDeContrasenaPendienteAsync(id)).Valor);
            Assert.Equal(MensajesIdentidad.CambioPendiente,
                Assert.Single((await proveedor.VerificarContrasenaAsync(usuario, "Temporal#2026x")).Errors()));
            Assert.Equal(HttpStatusCode.BadRequest, await PedirTokenDePruebaAsync(usuario, "Temporal#2026x"));

            Assert.True((await proveedor.FijarContrasenaAsync(id, "Definitiva#2026x", temporal: false)).IsSuccess);
            Assert.False((await proveedor.TieneCambioDeContrasenaPendienteAsync(id)).Valor);
            Assert.True((await proveedor.VerificarContrasenaAsync(usuario, "Definitiva#2026x")).IsSuccess);
            Assert.Equal(MensajesIdentidad.ContrasenaActualIncorrecta,
                Assert.Single((await proveedor.VerificarContrasenaAsync(usuario, "Incorrecta#2026")).Errors()));
            Assert.Equal(HttpStatusCode.OK, await PedirTokenDePruebaAsync(usuario, "Definitiva#2026x"));

            Assert.True((await proveedor.DeshabilitarAsync(id)).IsSuccess);
            Assert.Equal(MensajesIdentidad.CuentaDeshabilitada,
                Assert.Single((await proveedor.VerificarContrasenaAsync(usuario, "Definitiva#2026x")).Errors()));
            Assert.True((await proveedor.HabilitarAsync(id)).IsSuccess);
        }
        finally
        {
            Assert.True((await proveedor.EliminarUsuarioAsync(id)).IsSuccess);
        }

        Assert.True((await proveedor.TieneCambioDeContrasenaPendienteAsync(id)).IsError);
    }

    /// <summary>Lo mismo que haría el Front: pedir un token. Con contraseña temporal, Keycloak responde 400.</summary>
    private static async Task<HttpStatusCode> PedirTokenDePruebaAsync(string usuario, string contrasena)
    {
        using var http = new HttpClient();
        using var respuesta = await http.PostAsync($"{Authority}/protocol/openid-connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "libreria-pruebas",
            ["username"] = usuario,
            ["password"] = contrasena
        }));
        return respuesta.StatusCode;
    }
}
