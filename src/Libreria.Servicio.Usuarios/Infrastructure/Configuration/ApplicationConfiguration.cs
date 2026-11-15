using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Infrastructure.Configuration;

public sealed class ApplicationConfiguration
    : IApplicationConfiguration
{
    private readonly IConfiguration _configuration;

    public ApplicationConfiguration(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string BaseUrl =>
        _configuration["BASE_URL"]
        ?? throw new InvalidOperationException(
            "Falta la configuración BASE_URL.");
}