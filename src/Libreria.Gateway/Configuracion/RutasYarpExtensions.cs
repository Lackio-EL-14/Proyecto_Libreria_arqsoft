namespace Libreria.Gateway.Configuracion;

/// <summary>
/// Carga las rutas del gateway desde un archivo por grupo (sección 6.2 del planning):
/// <c>yarp.g1.json</c> (Grupo 1) y <c>yarp.g2.json</c> (Grupo 2). Ambos escriben en la
/// sección <c>ReverseProxy</c>; como los IDs de ruta y de cluster no se repiten, se fusionan.
/// </summary>
public static class RutasYarpExtensions
{
    public const string SeccionProxy = "ReverseProxy";

    public static readonly IReadOnlyList<string> ArchivosDeRutas = ["yarp.g1.json", "yarp.g2.json"];

    /// <summary>
    /// Agrega los archivos de rutas y, encima, vuelve a agregar <c>appsettings.{entorno}.json</c>,
    /// las variables de entorno y la línea de comandos, para que sigan ganando sobre los JSON de rutas
    /// (orden de precedencia: rutas &lt; appsettings del entorno &lt; entorno &lt; línea de comandos).
    /// Así un destino se cambia sin tocar los <c>yarp.*.json</c>, por ejemplo:
    /// <c>ReverseProxy__Clusters__productos__Destinations__principal__Address=http://localhost:5101</c>.
    /// </summary>
    public static IConfigurationBuilder AgregarRutasYarp(
        this IConfigurationBuilder configuracion, string[] args, string? entorno = null)
    {
        foreach (var archivo in ArchivosDeRutas)
        {
            configuracion.AddJsonFile(archivo, optional: false, reloadOnChange: true);
        }

        if (!string.IsNullOrWhiteSpace(entorno))
        {
            configuracion.AddJsonFile($"appsettings.{entorno}.json", optional: true, reloadOnChange: true);
        }

        return configuracion
            .AddEnvironmentVariables()
            .AddCommandLine(args);
    }
}
