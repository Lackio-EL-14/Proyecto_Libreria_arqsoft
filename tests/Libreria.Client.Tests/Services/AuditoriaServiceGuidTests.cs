using System.Net;
using Libreria.Client.Clients.Usuarios;
using Libreria.Client.DTOs.Productos;
using Libreria.Client.Services.Auditoria;
using Libreria.Client.Tests.Clients;

namespace Libreria.Client.Tests.Services;

/// <summary>
/// US-116: la auditoría de Clientes guarda el PublicId (GUID) del usuario. Los nombres se piden a
/// <c>api/usuarios/nombres</c>; si Usuarios aún no acepta GUID (US-124/US-127), la página no se cae.
/// </summary>
public sealed class AuditoriaServiceGuidTests
{
    private static readonly Guid Admin = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid Vendedor = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    private static readonly DateTime Alta = new(2026, 11, 14, 10, 0, 0);
    private static readonly DateTime Cambio = new(2026, 11, 15, 9, 30, 0);

    private static (AuditoriaService Servicio, ManejadorHttpFalso Manejador) Crear(HttpStatusCode estado, string cuerpo = "")
    {
        var manejador = new ManejadorHttpFalso(estado, cuerpo);
        var usuarios = new ServicioUsuariosClient(ManejadorHttpFalso.CrearHttp(manejador));
        return (new AuditoriaService(usuarios), manejador);
    }

    [Fact]
    public async Task Resuelve_los_nombres_por_GUID()
    {
        var (servicio, manejador) = Crear(HttpStatusCode.OK,
            $$"""{"{{Admin}}":"admin","{{Vendedor}}":"vendedor"}""");

        var auditoria = await servicio.ObtenerAsync(Admin, Alta, Vendedor, Cambio);

        Assert.Equal(
            $"http://localhost:5080/api/usuarios/nombres?ids={Admin}&ids={Vendedor}",
            manejador.UltimaPeticion!.RequestUri!.AbsoluteUri);
        Assert.Equal("admin", auditoria.CreadoPor);
        Assert.Equal(Alta, auditoria.FechaCreacion);
        Assert.Equal("vendedor", auditoria.ModificadoPor);
        Assert.Equal(Cambio, auditoria.FechaModificacion);
    }

    [Fact]
    public async Task Si_Usuarios_rechaza_los_GUID_muestra_usuario_no_disponible()
    {
        var (servicio, _) = Crear(HttpStatusCode.BadRequest,
            """{"title":"Solicitud inválida.","detail":"Los identificadores de usuario deben ser números positivos."}""");

        var auditoria = await servicio.ObtenerAsync(Admin, Alta, null, null);

        Assert.Equal("Usuario no disponible", auditoria.CreadoPor);
        Assert.Null(auditoria.ModificadoPor);
    }

    [Fact]
    public async Task Sin_modificacion_no_muestra_fecha_de_modificacion()
    {
        var (servicio, _) = Crear(HttpStatusCode.OK, $$"""{"{{Admin}}":"admin"}""");

        var auditoria = await servicio.ObtenerAsync(Admin, Alta, null, Cambio);

        Assert.Null(auditoria.ModificadoPor);
        Assert.Null(auditoria.FechaModificacion);
    }

    [Fact]
    public async Task Sin_usuarios_no_llama_al_servicio()
    {
        var (servicio, manejador) = Crear(HttpStatusCode.OK, "{}");

        var auditoria = await servicio.ObtenerAsync((Guid?)null, Alta, null, null);

        Assert.Null(manejador.UltimaPeticion);
        Assert.Null(auditoria.CreadoPor);
    }


    [Fact]
    public async Task Historico_resuelve_GUID_del_usuario_a_nombre()
    {
        var (servicio, manejador) = Crear(
            HttpStatusCode.OK,
            $$"""{"{{Admin}}":"Administrador Sistema"}""");

        var historial = new[]
        {
            new HistoricoCostoDto(
                Alta,
                11m,
                10m,
                1m,
                null,
                "Prueba US-123",
                Admin.ToString())
        };

        var resultado =
            await servicio
                .ResolverUsuariosHistoricoAsync(
                    historial);

        var registro = Assert.Single(resultado);

        Assert.Equal(
            "Administrador Sistema",
            registro.RegistradoPor);

        Assert.Contains(
            $"ids={Admin}",
            manejador.UltimaPeticion!
                .RequestUri!
                .Query);
    }
}
