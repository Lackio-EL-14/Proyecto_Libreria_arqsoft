using Libreria.Application.Ports.Secondary;
using Microsoft.Extensions.Configuration;

namespace Libreria.Infrastructure.Configuration;

public class ConfiguracionAplicacion : IConfiguracionAplicacion
{
    private readonly IConfiguration _configuration;

    public ConfiguracionAplicacion(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string BaseUrl
    {
        get
        {
            var baseUrl = _configuration["BASE_URL"];

            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException(
                    "Falta la configuración BASE_URL.");

            return baseUrl.TrimEnd('/');
        }
    }
}