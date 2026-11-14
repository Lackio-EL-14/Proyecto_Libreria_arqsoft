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
    /// Agrega los archivos de rutas y vuelve a agregar las variables de entorno y la línea
    /// de comandos, para que sigan ganando sobre los JSON. Así un destino se cambia sin tocar
    /// el repositorio, por ejemplo:
    /// <c>ReverseProxy__Clusters__productos__Destinations__principal__Address=http://localhost:5101</c>.
    /// </summary>
    public static IConfigurationBuilder AgregarRutasYarp(this IConfigurationBuilder configuracion, string[] args)
    {
        foreach (var archivo in ArchivosDeRutas)
        {
            configuracion.AddJsonFile(archivo, optional: false, reloadOnChange: true);
        }

        return configuracion
            .AddEnvironmentVariables()
            .AddCommandLine(args);
    }
}
