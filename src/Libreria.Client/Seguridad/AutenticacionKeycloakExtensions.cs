using Libreria.Client.DTOs.Usuarios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Libreria.Client.Seguridad;

/// <summary>
/// Login y sesiones con OpenID Connect contra Keycloak (US-125; docs/contratos/keycloak.md §4).
/// El Front no ve contraseñas: Keycloak muestra el login, exige el cambio de la contraseña
/// temporal (US-131) y emite los tokens; la cookie cifrada es la sesión local.
/// </summary>
public static class AutenticacionKeycloakExtensions
{
    public const string Seccion = "Keycloak";

    /// <summary>La cookie dura lo mismo que el token de acceso del realm (60 minutos).</summary>
    public static readonly TimeSpan DuracionSesion = TimeSpan.FromMinutes(60);

    public static IServiceCollection AddAutenticacionKeycloak(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment entorno)
    {
        var keycloak = configuration.GetSection(Seccion);
        var authority = Requerir(keycloak["Authority"], "Keycloak:Authority");
        var clientId = Requerir(keycloak["ClientId"], "Keycloak:ClientId");
        var clientSecret = Requerir(keycloak["ClientSecret"], "Keycloak__ClientSecret (user-secrets o variable de entorno)");

        services.AddScoped<ValidarEstadoUsuarioCookieEvents>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Cuenta/Login";
                options.LogoutPath = "/Cuenta/Logout";
                options.AccessDeniedPath = "/Cuenta/AccesoDenegado";
                options.Cookie.Name = "Libreria.Sesion";
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = DuracionSesion;
                options.SlidingExpiration = false;
                options.EventsType = typeof(ValidarEstadoUsuarioCookieEvents);
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = authority;
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.RequireHttpsMetadata = !entorno.IsDevelopment();

                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                // Respuesta por query + cookies Lax: funciona en http://localhost sin SameSite=None.
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SameSite = SameSiteMode.Lax;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.SaveTokens = true;              // access_token para BearerTokenDelegatingHandler
                options.UseTokenLifetime = true;        // la cookie vence con el token
                options.GetClaimsFromUserInfoEndpoint = false; // el id_token ya trae name, email y roles
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = ClaimsKeycloak.NombreUsuario,
                    RoleClaimType = ClaimsKeycloak.Roles,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                };

                // CallbackPath = /signin-oidc y SignedOutCallbackPath = /signout-callback-oidc (por defecto),
                // las mismas URI exactas registradas en el cliente libreria-front.
                options.SignedOutRedirectUri = MotivosSesion.UrlLogin(MotivosSesion.Cerrada);

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        // Sesión recién rechazada (vencida, inactiva…): al login con el mensaje,
                        // no de vuelta a Keycloak (evita el bucle con su sesión SSO).
                        if (context.HttpContext.Items[MotivosSesion.ClaveItem] is string motivo)
                        {
                            context.Response.Redirect(MotivosSesion.UrlLogin(motivo));
                            context.HandleResponse();
                            return Task.CompletedTask;
                        }

                        context.ProtocolMessage.UiLocales = "es";
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = context =>
                    {
                        context.Response.Redirect(MotivosSesion.UrlLogin(MotivosSesion.Error));
                        context.HandleResponse();
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PoliticasAcceso.SoloAdministrador, politica =>
                politica.RequireRole(RolesSistema.Administrador));

            options.AddPolicy(PoliticasAcceso.GestionClientes, politica =>
                politica.RequireRole(RolesSistema.Administrador, RolesSistema.Vendedor));
        });

        return services;
    }

    private static string Requerir(string? valor, string nombre) =>
        string.IsNullOrWhiteSpace(valor)
            ? throw new InvalidOperationException($"Falta configurar {nombre} (ver docs/contratos/keycloak.md y README).")
            : valor;
}
