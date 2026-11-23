namespace Libreria.Client.Seguridad;

/// <summary>
/// Pruebas del 22/11, caso D5: ninguna página generada por el servidor queda en la caché del
/// navegador, así que, después de cerrar sesión, «Atrás» no muestra una copia de una página
/// protegida. Se registra después de <c>UseStaticFiles</c>: CSS, JS e imágenes siguen en caché.
/// </summary>
public sealed class SinCacheMiddleware
{
    public const string CacheControl = "no-store, no-cache, must-revalidate";

    private readonly RequestDelegate _siguiente;

    public SinCacheMiddleware(RequestDelegate siguiente)
    {
        _siguiente = siguiente;
    }

    public Task InvokeAsync(HttpContext contexto)
    {
        // Al empezar la respuesta, para ganarle a los encabezados que ponga cualquier otro componente.
        contexto.Response.OnStarting(() =>
        {
            Aplicar(contexto.Response.Headers);
            return Task.CompletedTask;
        });

        return _siguiente(contexto);
    }

    public static void Aplicar(IHeaderDictionary encabezados)
    {
        encabezados.CacheControl = CacheControl;
        encabezados.Pragma = "no-cache";
        encabezados.Expires = "0";
    }
}
