using System.Net;
using System.Security.Claims;
using Libreria.Client.Pages.Cuenta;
using Libreria.Client.Seguridad;
using Libreria.Client.Tests.Infraestructura;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Client.Tests.Seguridad;

/// <summary>Pruebas del 22/11, caso D5: «Atrás» después de cerrar sesión no muestra páginas protegidas.</summary>
public sealed class SinCacheTests : IClassFixture<FrontFactory>
{
    private readonly FrontFactory _front;

    public SinCacheTests(FrontFactory front)
    {
        _front = front;
        _front.Gateway.Responder(HttpMethod.Get, "/api/productos/formulario", HttpStatusCode.OK, """{"categorias":[],"marcas":[]}""");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Clientes/Create")]
    [InlineData("/Productos/Create")]
    [InlineData("/Cuenta/CambiarContrasena")]
    public async Task Las_paginas_protegidas_no_se_guardan_en_cache(string url)
    {
        using var respuesta = await _front.CrearCliente().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(SinCacheMiddleware.CacheControl, respuesta.Headers.NonValidated["Cache-Control"].ToString());
        Assert.True(respuesta.Headers.CacheControl is { NoStore: true, NoCache: true, MustRevalidate: true });
        Assert.Contains("no-cache", respuesta.Headers.Pragma.ToString());
        Assert.Equal("0", string.Join(",", respuesta.Content.Headers.GetValues("Expires")));
    }

    [Fact]
    public async Task Los_archivos_estaticos_siguen_en_cache()
    {
        using var respuesta = await _front.CrearCliente().GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.DoesNotContain("no-store", respuesta.Headers.CacheControl?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task El_layout_recarga_la_pagina_si_viene_de_la_cache_de_atras()
    {
        var html = await _front.LeerPaginaAsync("/");

        Assert.Contains("addEventListener(\"pageshow\"", html);
        Assert.Contains("evento.persisted", html);
        Assert.Contains("window.location.reload()", html);
    }

    [Fact]
    public async Task Cerrar_sesion_sin_token_antiforgery_se_rechaza()
    {
        using var respuesta = await _front.CrearCliente().PostAsync("/Cuenta/Logout", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task El_boton_de_cerrar_sesion_es_un_post_con_token()
    {
        var html = await _front.LeerPaginaAsync("/");
        var formulario = System.Text.RegularExpressions.Regex.Match(html,
            "<form method=\"post\" class=\"logout-form\"[^>]*action=\"/Cuenta/Logout\">[\\s\\S]*?</form>");

        Assert.True(formulario.Success);
        Assert.Contains("__RequestVerificationToken", formulario.Value);
    }

    [Fact]
    public void Cerrar_sesion_borra_la_cookie_y_cierra_la_sesion_en_Keycloak()
    {
        var pagina = new LogoutModel
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimsKeycloak.Sub, "a0000000-0000-0000-0000-000000000001")], "Prueba"))
                }
            }
        };

        var resultado = Assert.IsType<SignOutResult>(pagina.OnPost());

        Assert.Equal(
            [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme],
            resultado.AuthenticationSchemes);
        Assert.Equal(MotivosSesion.UrlLogin(MotivosSesion.Cerrada), resultado.Properties?.RedirectUri);
    }

    [Fact]
    public void Cerrar_sesion_por_GET_no_hace_nada()
    {
        Assert.IsType<RedirectToPageResult>(new LogoutModel().OnGet());
    }
}
