using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Libreria.Servicio.Productos.Infrastructure.Seguridad;

/// <summary>
/// Validación del JWT de Keycloak (US-126; docs/contratos/keycloak.md §4). Copiado en cada
/// servicio: no hay bibliotecas compartidas entre microservicios.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Firma RS256 con las claves públicas del realm (JWKS de <c>Keycloak:Authority</c>), emisor,
/// audiencia <c>libreria-api</c> y vencimiento: si algo falla, <b>401</b>.</item>
/// <item><b>Todo exige token</b> (política por defecto); solo se exceptúa lo marcado con
/// <c>AllowAnonymous</c>, como <c>/salud</c>.</item>
/// <item>Roles del claim plano <c>roles</c>: rol insuficiente → <b>403</b>.</item>
/// </list>
/// </remarks>
public static class SeguridadJwtExtensions
{
    public const string Seccion = "Keycloak";

    /// <summary>Solo el rol Administrador.</summary>
    public const string SoloAdministrador = "SoloAdministrador";

    /// <summary>Administrador o Vendedor (gestión de clientes).</summary>
    public const string GestionClientes = "GestionClientes";

    public const string RolAdministrador = "Administrador";
    public const string RolVendedor = "Vendedor";

    public static IServiceCollection AddSeguridadJwt(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment entorno)
    {
        var authority = Requerir(configuration[$"{Seccion}:Authority"], "Keycloak:Authority");
        var audience = Requerir(configuration[$"{Seccion}:Audience"], "Keycloak:Audience");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = !entorno.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authority,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ExigirSujeto,
                    OnChallenge = ResponderNoAutenticadoAsync,
                    OnForbidden = ResponderSinPermisoAsync
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(SoloAdministrador, politica => politica.RequireRole(RolAdministrador));
            options.AddPolicy(GestionClientes, politica => politica.RequireRole(RolAdministrador, RolVendedor));
        });

        return services;
    }

    /// <summary>
    /// La bitácora guarda el <c>sub</c> del token (US-127): un token sin un <c>sub</c> GUID no
    /// identifica al usuario y se rechaza con 401, en lugar de fallar después al auditar.
    /// </summary>
    private static Task ExigirSujeto(TokenValidatedContext context)
    {
        var sub = context.Principal?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var id) || id == Guid.Empty)
        {
            context.Fail("El token no identifica al usuario (sub).");
        }

        return Task.CompletedTask;
    }

    private static Task ResponderNoAutenticadoAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        var vencido = context.AuthenticateFailure is SecurityTokenExpiredException;
        context.Response.Headers.WWWAuthenticate = context.AuthenticateFailure is null
            ? "Bearer"
            : $"Bearer error=\"invalid_token\"{(vencido ? ", error_description=\"The token expired\"" : string.Empty)}";

        return EscribirProblemaAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "No autenticado.",
            context.AuthenticateFailure switch
            {
                null => "Falta el token de acceso (Authorization: Bearer).",
                SecurityTokenExpiredException => "El token de acceso venció. Inicie sesión de nuevo.",
                _ => "El token de acceso no es válido (firma, emisor o audiencia)."
            });
    }

    private static Task ResponderSinPermisoAsync(ForbiddenContext context) =>
        EscribirProblemaAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Acceso denegado.",
            "Su rol no tiene permiso para esta operación.");

    private static Task EscribirProblemaAsync(HttpContext http, int estado, string titulo, string detalle)
    {
        http.Response.StatusCode = estado;
        http.Response.ContentType = "application/problem+json";
        var problema = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle };
        return http.Response.WriteAsync(JsonSerializer.Serialize(problema, JsonSerializerOptions.Web));
    }

    private static string Requerir(string? valor, string clave) =>
        string.IsNullOrWhiteSpace(valor)
            ? throw new InvalidOperationException($"Falta configurar {clave} (ver docs/contratos/keycloak.md).")
            : valor.TrimEnd('/');
}
