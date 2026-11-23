using System.Net;
using System.Text.RegularExpressions;
using Libreria.Client.Tests.Infraestructura;

namespace Libreria.Client.Tests.Formularios;

/// <summary>
/// Pruebas del 22/11, casos A4 y B2 (y el resto de formularios): sin validación nativa del navegador
/// («Completa este campo»), con validación del cliente (jQuery unobtrusive) y con mensajes propios
/// por campo, en español, también cuando el POST llega vacío al servidor.
/// </summary>
public sealed class FormulariosValidacionTests : IClassFixture<FrontFactory>
{
    private const string ClienteId = "c0000000-0000-0000-0000-000000000002";
    private const string ProveedorId = "f0000000-0000-0000-0000-000000000001";

    private readonly FrontFactory _front;

    public FormulariosValidacionTests(FrontFactory front)
    {
        _front = front;
        _front.Gateway.Responder(HttpMethod.Get, "/api/productos/formulario", HttpStatusCode.OK, """{"categorias":[],"marcas":[]}""");
        _front.Gateway.Responder(HttpMethod.Get, "/api/usuarios/roles", HttpStatusCode.OK, """["Administrador","Vendedor"]""");
        _front.Gateway.Responder(HttpMethod.Get, "/api/usuarios/nombres", HttpStatusCode.OK, "{}");
        _front.Gateway.Responder(HttpMethod.Get, $"/api/clientes/{ClienteId}", HttpStatusCode.OK,
            $$"""{"publicId":"{{ClienteId}}","ciNit":"4567890","razonSocial":"María Fernanda Quispe Choque","correo":null,"estado":true,"fechaCreacion":"2026-11-01T09:10:00","usuarioCreacionId":null,"fechaModificacion":null,"usuarioModificacionId":null}""");
        _front.Gateway.Responder(HttpMethod.Get, $"/api/proveedores/{ProveedorId}", HttpStatusCode.OK,
            $$"""{"publicId":"{{ProveedorId}}","nit":"1020304050","razonSocial":"Distribuidora Escolar Andina","telefono":null,"correo":null,"estado":true,"nombreContacto":"Ana Pérez","cargoContacto":null,"telefonoContacto":"70000000","correoContacto":null,"fechaCreacion":"2026-11-01T09:00:00","usuarioCreacionId":null,"fechaModificacion":null,"usuarioModificacionId":null}""");
    }

    public static TheoryData<string> Formularios =>
    [
        "/Clientes/Create",
        "/Clientes/Edit/" + ClienteId,
        "/Proveedores/Create",
        "/Proveedores/Edit/" + ProveedorId,
        "/Productos/Create",
        "/Usuarios/Create",
        "/Cuenta/CambiarContrasena"
    ];

    [Theory]
    [MemberData(nameof(Formularios))]
    public async Task El_formulario_usa_validacion_propia_y_no_la_del_navegador(string url)
    {
        var html = await _front.LeerPaginaAsync(url);
        var formularios = Regex.Matches(html, "<form[^>]*>").Select(m => m.Value).ToList();

        Assert.NotEmpty(formularios);
        Assert.All(formularios, form => Assert.Contains("novalidate", form));
        Assert.DoesNotMatch(@"<(input|select|textarea)[^>]*\srequired[\s>=/]", html);
        Assert.Contains("/lib/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js", html);
        Assert.Contains("/js/validacion.js", html);
        Assert.Contains("data-val-required=", html);
        Assert.DoesNotMatch("<button[^>]*type=\"submit\"[^>]*disabled", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
    }

    [Fact]
    public async Task A4_cliente_vacio_muestra_los_mensajes_del_sistema_por_campo()
    {
        var (respuesta, html) = await EnviarVacioAsync("/Clientes/Create", "Input.CiNit", "Input.RazonSocial", "Input.Correo");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("data-valmsg-for=\"Input.CiNit\" data-valmsg-replace=\"true\">El CI/NIT es obligatorio.</span>", html);
        Assert.Contains("data-valmsg-for=\"Input.RazonSocial\" data-valmsg-replace=\"true\">La razón social es obligatoria.</span>", html);
        Assert.Contains("CI/NIT <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Correo electrónico (opcional)</label>", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
    }

    [Fact]
    public async Task A4_el_cliente_tiene_los_mensajes_del_sistema_en_el_navegador()
    {
        var html = await _front.LeerPaginaAsync("/Clientes/Create");

        Assert.Contains("data-val-required=\"El CI/NIT es obligatorio.\"", html);
        Assert.Contains("data-val-required=\"La razón social es obligatoria.\"", html);
        Assert.Contains("data-val-email=\"Ingrese un correo electrónico válido.\"", html);
    }

    [Fact]
    public async Task B2_proveedor_vacio_muestra_nit_razon_social_contacto_y_medio_de_contacto()
    {
        var (respuesta, html) = await EnviarVacioAsync("/Proveedores/Create",
            "Input.Nit", "Input.RazonSocial", "Input.Telefono", "Input.Correo",
            "Input.NombreContacto", "Input.CargoContacto", "Input.TelefonoContacto", "Input.CorreoContacto");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("El NIT es obligatorio.</span>", html);
        Assert.Contains("La razón social es obligatoria.</span>", html);
        Assert.Contains("El nombre del contacto es obligatorio.</span>", html);
        Assert.Contains("data-valmsg-for=\"Input.TelefonoContacto\" data-valmsg-replace=\"true\">Ingrese al menos un teléfono o correo para el contacto principal.</span>", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
    }

    [Fact]
    public async Task B2_el_medio_de_contacto_tambien_se_valida_en_el_navegador()
    {
        var html = await _front.LeerPaginaAsync("/Proveedores/Create");

        Assert.Contains("data-val-requeridoalguno=\"Ingrese al menos un teléfono o correo para el contacto principal.\"", html);
        Assert.Contains("data-val-requeridoalguno-otro=\"*.CorreoContacto\"", html);
        Assert.Contains("data-val-required=\"El nombre del contacto es obligatorio.\"", html);
        Assert.Contains("NIT <span class=\"text-danger\">*</span></label>", html);
        Assert.Contains("Teléfono (opcional)</label>", html);
        Assert.Contains("Correo electrónico (opcional)</label>", html);
        Assert.Contains("Teléfono</label>", html);
    }

    [Fact]
    public async Task Proveedor_con_correo_de_contacto_no_exige_telefono()
    {
        var (_, html) = await _front.EnviarFormularioAsync(_front.CrearCliente(), "/Proveedores/Create",
            new Dictionary<string, string> { ["Input.Nit"] = "", ["Input.CorreoContacto"] = "ana@correo.bo" });

        Assert.DoesNotContain("Ingrese al menos un teléfono o correo para el contacto principal.</span>", html);
        Assert.Contains("El NIT es obligatorio.</span>", html);
    }

    [Fact]
    public async Task Usuario_vacio_muestra_los_mensajes_por_campo()
    {
        var (respuesta, html) = await _front.EnviarFormularioAsync(_front.CrearCliente(), "/Usuarios/Create",
            new Dictionary<string, string> { ["Nombre"] = "", ["Apellido"] = "", ["Correo"] = "", ["Rol"] = "" },
            handler: "Crear");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("El nombre es obligatorio.</span>", html);
        Assert.Contains("El apellido paterno es obligatorio.</span>", html);
        Assert.Contains("El correo es obligatorio.</span>", html);
        Assert.Contains("Debe seleccionar un rol válido.</span>", html);
        Assert.Contains("Apellido materno (opcional)</label>", html);
        Assert.Contains("Correo electrónico <span class=\"text-danger\">*</span></label>", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
    }

    [Fact]
    public async Task Cambio_de_contrasena_vacio_muestra_los_mensajes_por_campo()
    {
        var (respuesta, html) = await EnviarVacioAsync("/Cuenta/CambiarContrasena",
            "Input.ContrasenaActual", "Input.NuevaContrasena", "Input.Confirmacion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("Ingrese su contraseña actual.</span>", html);
        Assert.Contains("Ingrese la nueva contraseña.</span>", html);
        Assert.Contains("Ingrese la confirmación de la nueva contraseña.</span>", html);
        Assert.Contains("Contraseña actual <span class=\"text-danger\">*</span></label>", html);
        TextosVisibles.SinNombresInternosNiIngles(html);
    }

    [Fact]
    public async Task Un_post_vacio_no_llega_a_los_microservicios()
    {
        await EnviarVacioAsync("/Clientes/Create", "Input.CiNit", "Input.RazonSocial");
        await EnviarVacioAsync("/Proveedores/Create", "Input.Nit", "Input.RazonSocial", "Input.NombreContacto");

        Assert.DoesNotContain(_front.Gateway.Recibidas, r => r.Metodo is "POST" or "PUT");
    }

    private Task<(HttpResponseMessage Respuesta, string Html)> EnviarVacioAsync(string url, params string[] campos) =>
        _front.EnviarFormularioAsync(_front.CrearCliente(), url, campos.ToDictionary(campo => campo, _ => string.Empty));
}
