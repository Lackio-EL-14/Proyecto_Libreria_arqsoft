using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Libreria.Servicio.Productos.Infrastructure.Seguridad;

/// <summary>Botón «Authorize» de Swagger con el token de Keycloak (US-126).</summary>
public static class SwaggerBearerExtensions
{
    private const string Esquema = "Bearer";

    public static void AgregarBearerDeKeycloak(this SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(Esquema, new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token de acceso de Keycloak (realm libreria, audiencia libreria-api). " +
                          "Obténgalo con el cliente libreria-pruebas: docs/contratos/keycloak.md §5."
        });

        options.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(Esquema, documento)] = []
        });
    }
}
