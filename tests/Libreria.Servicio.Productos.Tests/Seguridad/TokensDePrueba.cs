using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Libreria.Servicio.Productos.Tests.Seguridad;

/// <summary>
/// Tokens RS256 como los de Keycloak, firmados con una clave local (US-126). El servicio bajo
/// prueba conserva toda su validación (emisor, audiencia, vencimiento, firma); solo cambia de
/// dónde salen las claves públicas: de esta clave y no del JWKS del realm.
/// </summary>
public static class TokensDePrueba
{
    public const string Emisor = "http://localhost:8080/realms/libreria";
    public const string Audiencia = "libreria-api";

    public static readonly Guid SubAdmin = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid SubVendedor = Guid.Parse("a0000000-0000-0000-0000-000000000002");

    private static readonly RsaSecurityKey Clave = NuevaClave("clave-del-realm-de-prueba");
    private static readonly RsaSecurityKey ClaveAjena = NuevaClave("clave-ajena");

    public static string Admin() => Crear(SubAdmin, "admin", ["Administrador"]);

    public static string Vendedor() => Crear(SubVendedor, "vendedor", ["Vendedor"]);

    public static string Vencido() =>
        Crear(SubAdmin, "admin", ["Administrador"], emitido: DateTime.UtcNow.AddHours(-2), vence: DateTime.UtcNow.AddMinutes(-5));

    public static string AudienciaEquivocada() => Crear(SubAdmin, "admin", ["Administrador"], audiencia: "otra-api");

    public static string EmisorEquivocado() =>
        Crear(SubAdmin, "admin", ["Administrador"], emisor: "http://localhost:8080/realms/master");

    public static string FirmadoConOtraClave() => Crear(SubAdmin, "admin", ["Administrador"], clave: ClaveAjena);

    /// <summary>Token válido de Administrador con un carácter de la firma cambiado.</summary>
    public static string FirmaAlterada()
    {
        var token = Admin();
        var inicioFirma = token.LastIndexOf('.') + 1;
        var posicion = inicioFirma + (token.Length - inicioFirma) / 2;
        var cambiado = token[posicion] == 'A' ? 'B' : 'A';
        return string.Concat(token.AsSpan(0, posicion), cambiado.ToString(), token.AsSpan(posicion + 1));
    }

    public static string Crear(
        Guid sub,
        string usuario,
        string[] roles,
        string? audiencia = Audiencia,
        string emisor = Emisor,
        DateTime? emitido = null,
        DateTime? vence = null,
        RsaSecurityKey? clave = null)
    {
        var ahora = emitido ?? DateTime.UtcNow;
        var claims = new Dictionary<string, object>
        {
            ["sub"] = sub.ToString(),
            ["preferred_username"] = usuario,
            ["name"] = usuario,
            ["roles"] = roles
        };
        if (audiencia is not null)
        {
            claims["aud"] = audiencia;
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = emisor,
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = vence ?? ahora.AddMinutes(60),
            Claims = claims,
            SigningCredentials = new SigningCredentials(clave ?? Clave, SecurityAlgorithms.RsaSha256)
        });
    }

    public static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    /// <summary>Hace que el JwtBearer del servicio confíe en <see cref="Clave"/> en lugar del JWKS de Keycloak.</summary>
    public static IServiceCollection UsarJwtDePrueba(this IServiceCollection servicios) =>
        servicios.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opciones =>
        {
            var configuracion = new OpenIdConnectConfiguration { Issuer = Emisor };
            configuracion.SigningKeys.Add(Clave);
            opciones.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuracion);
        });

    private static RsaSecurityKey NuevaClave(string id) => new(RSA.Create(2048)) { KeyId = id };
}
