using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Libreria.Client.Adapters.DependencyInjection;
using Libreria.Client.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Libreria.Client.Tests.Infraestructura;

/// <summary>
/// El Front completo en memoria (pruebas del 22/11): sesión de administrador simulada y un
/// gateway falso que responde lo que cada prueba necesite. Sirve para revisar el HTML real
/// (etiquetas, mensajes, data-val-*, novalidate) y los encabezados de las respuestas.
/// </summary>
public sealed class FrontFactory : WebApplicationFactory<Program>
{
    public const string Esquema = "Prueba";

    public GatewayFalso Gateway { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Keycloak:Authority", "http://keycloak.prueba/realms/libreria");
        builder.UseSetting("Keycloak:ClientId", "libreria-front");
        builder.UseSetting("Keycloak:ClientSecret", "solo-pruebas");
        builder.UseSetting("Servicios:Gateway:BaseUrl", "http://gateway.prueba/");

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, AutenticacionPrueba>(Esquema, _ => { });
            services.PostConfigure<AuthenticationOptions>(opciones =>
            {
                opciones.DefaultScheme = Esquema;
                opciones.DefaultAuthenticateScheme = Esquema;
                opciones.DefaultChallengeScheme = Esquema;
                opciones.DefaultSignOutScheme = Esquema;
            });
            services.AddHttpClient(ApiGateway.NombreCliente)
                .ConfigurePrimaryHttpMessageHandler(() => Gateway);
        });
    }

    /// <summary>Lee el token antiforgery de un formulario ya cargado (la cookie la guarda el cliente).</summary>
    public static string TokenAntiforgery(string html)
    {
        var coincidencia = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(coincidencia.Success, "La página no tiene token antiforgery.");
        return coincidencia.Groups[1].Value;
    }

    /// <summary>GET del formulario y POST con los campos indicados (vacíos por defecto) y su token.</summary>
    public async Task<(HttpResponseMessage Respuesta, string Html)> EnviarFormularioAsync(
        HttpClient cliente, string url, IDictionary<string, string>? campos = null, string? handler = null)
    {
        var formulario = await cliente.GetStringAsync(url);
        var valores = new Dictionary<string, string>(campos ?? new Dictionary<string, string>())
        {
            ["__RequestVerificationToken"] = TokenAntiforgery(formulario)
        };

        var destino = handler is null ? url : $"{url}?handler={handler}";
        var respuesta = await cliente.PostAsync(destino, new FormUrlEncodedContent(valores));
        return (respuesta, WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync()));
    }

    /// <summary>GET de una página con el HTML decodificado (Razor escribe «í» como «&amp;#xED;»).</summary>
    public async Task<string> LeerPaginaAsync(string url) =>
        WebUtility.HtmlDecode(await CrearCliente().GetStringAsync(url));

    public HttpClient CrearCliente() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private sealed class AutenticacionPrueba(
        IOptionsMonitor<AuthenticationSchemeOptions> opciones, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(opciones, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identidad = new ClaimsIdentity(
                [
                    new Claim(ClaimsKeycloak.Sub, "a0000000-0000-0000-0000-000000000001"),
                    new Claim(ClaimsKeycloak.NombreUsuario, "admin"),
                    new Claim(ClaimsKeycloak.Nombre, "Administrador Sistema"),
                    new Claim(ClaimsKeycloak.Roles, "Administrador")
                ],
                Esquema,
                ClaimsKeycloak.NombreUsuario,
                ClaimsKeycloak.Roles);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema)));
        }
    }
}

/// <summary>Gateway falso: responde por método y ruta; lo no previsto devuelve 404. Registra lo recibido.</summary>
public sealed class GatewayFalso : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpResponseMessage>> _respuestas = new(StringComparer.OrdinalIgnoreCase);

    public List<(string Metodo, string Ruta, string Cuerpo)> Recibidas { get; } = [];

    public void Responder(HttpMethod metodo, string ruta, HttpStatusCode estado, string? json = null) =>
        _respuestas[$"{metodo} {ruta}"] = () => new HttpResponseMessage(estado)
        {
            Content = json is null ? null : new StringContent(json, Encoding.UTF8, "application/json")
        };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var ruta = request.RequestUri!.AbsolutePath;
        var cuerpo = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
        lock (Recibidas)
        {
            Recibidas.Add((request.Method.Method, ruta, cuerpo));
        }

        return _respuestas.TryGetValue($"{request.Method} {ruta}", out var respuesta)
            ? respuesta()
            : new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
