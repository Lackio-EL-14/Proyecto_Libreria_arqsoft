using System.Net;
using Libreria.Client.Clients.Ventas;

namespace Libreria.Client.Tests.Clients;

public sealed class ServicioVentasClientTests
{
    private static readonly Guid PublicId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task El_listado_envia_los_filtros_con_fechas_ISO()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK, "[]");
        var cliente = new ServicioVentasClient(ManejadorHttpFalso.CrearHttp(manejador));

        await cliente.ObtenerVentasAsync("Anulada", new DateOnly(2026, 11, 1), null);

        Assert.Equal(
            "/api/ventas?estado=Anulada&fechaDesde=2026-11-01",
            manejador.UltimaPeticion!.RequestUri!.PathAndQuery);
    }

    [Fact]
    public async Task Anular_envia_el_motivo_en_el_cuerpo()
    {
        var manejador = new ManejadorHttpFalso(HttpStatusCode.OK);
        var cliente = new ServicioVentasClient(ManejadorHttpFalso.CrearHttp(manejador));

        var resultado = await cliente.AnularVentaAsync(PublicId, "Error de cobro");

        Assert.True(resultado.Exitoso);
        Assert.Equal(HttpMethod.Post, manejador.UltimaPeticion!.Method);
        Assert.Equal($"/api/ventas/{PublicId}/anular", manejador.UltimaPeticion.RequestUri!.AbsolutePath);
        Assert.Equal("""{"motivo":"Error de cobro"}""", manejador.UltimoCuerpo);
    }

    [Fact]
    public async Task Un_comprobante_inexistente_devuelve_null()
    {
        var cliente = new ServicioVentasClient(
            ManejadorHttpFalso.CrearHttp(new ManejadorHttpFalso(HttpStatusCode.NotFound)));

        Assert.Null(await cliente.ObtenerComprobanteAsync(PublicId));
    }
}
