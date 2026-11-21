using System.Net;
using Libreria.Client.Adapters.Usuarios;
using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Usuarios;
using Libreria.Client.Tests.Clients;

namespace Libreria.Client.Tests.Adapters;

/// <summary>
/// US-140: el adaptador de Usuarios arma las rutas del gateway y traduce sus respuestas.
/// Sus páginas reciben los rechazos como <see cref="ApiClientException"/> con el mensaje del ProblemDetails.
/// </summary>
public sealed class UsuariosApiAdapterTests
{
    private static readonly Guid PublicId = Guid.Parse("a0000000-0000-0000-0000-000000000002");

    private static (UsuariosApiAdapter Adaptador, ManejadorHttpFalso Manejador) Crear(
        HttpStatusCode estado, string cuerpo = "")
    {
        var manejador = new ManejadorHttpFalso(estado, cuerpo);
        return (new UsuariosApiAdapter(ManejadorHttpFalso.CrearFabrica(manejador)), manejador);
    }

    [Fact]
    public async Task Un_200_se_traduce_a_la_lista_de_DTO()
    {
        var (adaptador, manejador) = Crear(
            HttpStatusCode.OK,
            """[{"publicId":"a0000000-0000-0000-0000-000000000002","fullName":"Ana Peña","username":"apena","email":"ana@libreria.bo","role":"Vendedor","status":"Activo"}]""");

        var usuarios = await adaptador.ObtenerUsuariosAsync("Activo");

        Assert.Equal("http://localhost:5080/api/usuarios?estado=Activo", manejador.UltimaPeticion!.RequestUri!.AbsoluteUri);
        var leido = Assert.Single(usuarios);
        Assert.Equal(PublicId, leido.PublicId);
        Assert.Equal("Ana Peña", leido.FullName);
    }

    [Fact]
    public async Task Un_200_del_alta_devuelve_el_usuario_creado()
    {
        var (adaptador, manejador) = Crear(
            HttpStatusCode.Created,
            """{"publicId":"a0000000-0000-0000-0000-000000000002","username":"apena"}""");

        var creado = await adaptador.CrearUsuarioAsync(new CreateUserRequestDto("Ana", "Peña", "ana@libreria.bo", "Vendedor"));

        Assert.Equal(HttpMethod.Post, manejador.UltimaPeticion!.Method);
        Assert.Equal("apena", creado!.Username);
    }

    [Fact]
    public async Task Un_400_llega_como_ApiClientException_con_el_mensaje_y_los_errores()
    {
        var (adaptador, _) = Crear(
            HttpStatusCode.BadRequest,
            """{"title":"Datos inválidos","detail":"La contraseña nueva no cumple la política.","status":400,"errors":{"NewPassword":["Debe tener al menos 8 caracteres."]}}""");

        var error = await Assert.ThrowsAsync<ApiClientException>(
            () => adaptador.CambiarContrasenaAsync(new ChangePasswordRequestDto("actual", "corta", "corta")));

        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Equal("La contraseña nueva no cumple la política.", error.Message);
        Assert.Equal("Debe tener al menos 8 caracteres.", Assert.Single(error.Errors!["NewPassword"]));
    }

    [Fact]
    public async Task Un_400_en_los_nombres_de_auditoria_no_rompe_la_pagina()
    {
        var (adaptador, _) = Crear(HttpStatusCode.BadRequest, """{"title":"GUID inválido"}""");

        var nombres = await adaptador.ObtenerNombresAsync(new[] { PublicId });

        Assert.Empty(nombres);
    }

    [Fact]
    public async Task Un_404_en_la_baja_llega_como_ApiClientException()
    {
        var (adaptador, manejador) = Crear(
            HttpStatusCode.NotFound,
            """{"title":"No encontrado","detail":"El usuario no existe.","status":404}""");

        var error = await Assert.ThrowsAsync<ApiClientException>(() => adaptador.DarDeBajaUsuarioAsync(PublicId));

        Assert.Equal(HttpStatusCode.NotFound, error.StatusCode);
        Assert.Equal("El usuario no existe.", error.Message);
        Assert.Equal(HttpMethod.Delete, manejador.UltimaPeticion!.Method);
        Assert.Equal($"http://localhost:5080/api/usuarios/{PublicId}", manejador.UltimaPeticion.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Un_503_lleva_a_servicio_no_disponible()
    {
        var (adaptador, _) = Crear(HttpStatusCode.ServiceUnavailable);

        var error = await Assert.ThrowsAsync<ServicioNoDisponibleException>(() => adaptador.ObtenerRolesAsync());

        Assert.Equal("Usuarios", error.Servicio);
    }

    [Fact]
    public async Task El_listado_envia_estado_orden_y_direccion_al_microservicio()
    {
        var (adaptador, manejador) = Crear(HttpStatusCode.OK, "[]");

        await adaptador.ObtenerUsuariosAsync("Pendiente", "correo", descendente: true);

        Assert.Equal("?estado=Pendiente&orden=correo&descendente=true", manejador.UltimaPeticion!.RequestUri!.Query);
    }
}
