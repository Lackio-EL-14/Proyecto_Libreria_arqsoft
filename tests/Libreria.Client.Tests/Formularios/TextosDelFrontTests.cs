using System.Net;
using Libreria.Client.Tests.Infraestructura;

namespace Libreria.Client.Tests.Formularios;

/// <summary>
/// Revisión de las pruebas del 22/11: ninguna página del Front muestra textos en inglés ni
/// nombres internos de propiedades (palabras pegadas en CamelCase).
/// </summary>
public sealed class TextosDelFrontTests : IClassFixture<FrontFactory>
{
    private readonly FrontFactory _front;

    public TextosDelFrontTests(FrontFactory front)
    {
        _front = front;
        _front.Gateway.Responder(HttpMethod.Get, "/api/clientes", HttpStatusCode.OK, "[]");
        _front.Gateway.Responder(HttpMethod.Get, "/api/proveedores", HttpStatusCode.OK, "[]");
        _front.Gateway.Responder(HttpMethod.Get, "/api/productos", HttpStatusCode.OK, """{"productos":[],"categorias":[],"marcas":[]}""");
        _front.Gateway.Responder(HttpMethod.Get, "/api/productos/formulario", HttpStatusCode.OK, """{"categorias":[],"marcas":[]}""");
        _front.Gateway.Responder(HttpMethod.Get, "/api/categorias", HttpStatusCode.OK, "[]");
        _front.Gateway.Responder(HttpMethod.Get, "/api/marcas", HttpStatusCode.OK, "[]");
        _front.Gateway.Responder(HttpMethod.Get, "/api/usuarios", HttpStatusCode.OK, "[]");
        _front.Gateway.Responder(HttpMethod.Get, "/api/usuarios/roles", HttpStatusCode.OK, """["Administrador","Vendedor"]""");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Clientes")]
    [InlineData("/Proveedores")]
    [InlineData("/Productos")]
    [InlineData("/Categorias")]
    [InlineData("/Marcas")]
    [InlineData("/Usuarios")]
    [InlineData("/Usuarios/Create")]
    [InlineData("/Cuenta/CambiarContrasena")]
    [InlineData("/Cuenta/AccesoDenegado")]
    [InlineData("/ServicioNoDisponible")]
    public async Task La_pagina_no_muestra_ingles_ni_nombres_internos(string url)
    {
        using var respuesta = await _front.CrearCliente().GetAsync(url);
        var html = WebUtility.HtmlDecode(await respuesta.Content.ReadAsStringAsync());

        // Acceso denegado responde 403 a propósito; las demás, 200.
        Assert.True(respuesta.StatusCode is HttpStatusCode.OK or HttpStatusCode.Forbidden, $"{url}: {respuesta.StatusCode}");
        TextosVisibles.SinNombresInternosNiIngles(html);
        Assert.DoesNotMatch(@"\b[A-ZÁÉÍÓÚÑa-záéíóúñ][a-záéíóúñ]+[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+", TextosVisibles.TextoSinCodigo(html));
    }
}
