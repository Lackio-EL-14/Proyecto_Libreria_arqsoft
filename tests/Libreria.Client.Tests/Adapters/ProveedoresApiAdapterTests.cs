using System.Net;
using Libreria.Client.Adapters.Proveedores;
using Libreria.Client.Clients.Common;
using Libreria.Client.DTOs.Proveedores;
using Libreria.Client.Tests.Clients;

namespace Libreria.Client.Tests.Adapters;

/// <summary>US-140: el adaptador de Proveedores arma las rutas del gateway y traduce sus respuestas.</summary>
public sealed class ProveedoresApiAdapterTests
{
    private static readonly Guid PublicId = Guid.Parse("b0000000-0000-0000-0000-000000000003");

    private const string ProveedorJson = """
        {"publicId":"b0000000-0000-0000-0000-000000000003","nit":"1023456019",
         "razonSocial":"Distribuidora Andina S.R.L.","telefono":"22401234","correo":"ventas@andina.bo",
         "estado":true,"nombreContacto":"José Mamani","cargoContacto":"Gerente","telefonoContacto":null,
         "correoContacto":null,"fechaCreacion":"2026-11-17T09:00:00",
         "usuarioCreacionId":"a0000000-0000-0000-0000-000000000001",
         "fechaModificacion":null,"usuarioModificacionId":null}
        """;

    private static (ProveedoresApiAdapter Adaptador, ManejadorHttpFalso Manejador) Crear(
        HttpStatusCode estado, string cuerpo = "")
    {
        var manejador = new ManejadorHttpFalso(estado, cuerpo);
        return (new ProveedoresApiAdapter(ManejadorHttpFalso.CrearFabrica(manejador)), manejador);
    }

    [Fact]
    public async Task Un_200_se_traduce_a_la_lista_de_DTO()
    {
        var (adaptador, manejador) = Crear(HttpStatusCode.OK, $"[{ProveedorJson}]");

        var proveedores = await adaptador.ObtenerProveedoresAsync(
            new FiltroProveedores("Andina", EstadoFiltroProveedores.Inactivos));

        Assert.Equal(HttpMethod.Get, manejador.UltimaPeticion!.Method);
        Assert.Equal(
            "http://localhost:5080/api/proveedores?busqueda=Andina&estado=false",
            manejador.UltimaPeticion.RequestUri!.AbsoluteUri);
        var leido = Assert.Single(proveedores);
        Assert.Equal(PublicId, leido.PublicId);
        Assert.Equal("José Mamani", leido.NombreContacto);
    }

    [Fact]
    public async Task Un_400_con_ValidationProblemDetails_llega_como_errores_del_formulario()
    {
        var (adaptador, manejador) = Crear(
            HttpStatusCode.BadRequest,
            """{"title":"Datos inválidos","status":400,"errors":{"Nit":["El NIT ya está registrado."]}}""");

        var resultado = await adaptador.RegistrarProveedorAsync(new ProveedorInput { Nit = "1023456019" });

        Assert.False(resultado.Exitoso);
        Assert.Equal("El NIT ya está registrado.", resultado.Errores["Input.Nit"]);
        Assert.Equal(HttpMethod.Post, manejador.UltimaPeticion!.Method);
        Assert.Contains("\"nit\":\"1023456019\"", manejador.UltimoCuerpo);
    }

    [Fact]
    public async Task Un_404_en_la_lectura_devuelve_null()
    {
        var (adaptador, manejador) = Crear(HttpStatusCode.NotFound);

        Assert.Null(await adaptador.ObtenerProveedorAsync(PublicId));
        Assert.Equal($"http://localhost:5080/api/proveedores/{PublicId}", manejador.UltimaPeticion!.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task Un_404_en_una_escritura_es_NoEncontrado()
    {
        var (adaptador, manejador) = Crear(HttpStatusCode.NotFound);

        var resultado = await adaptador.CambiarEstadoProveedorAsync(PublicId, false);

        Assert.True(resultado.NoEncontrado);
        Assert.Equal(HttpMethod.Patch, manejador.UltimaPeticion!.Method);
        Assert.Equal("""{"activo":false}""", manejador.UltimoCuerpo);
    }

    [Fact]
    public async Task Un_503_en_la_lectura_lleva_a_servicio_no_disponible()
    {
        var (adaptador, _) = Crear(HttpStatusCode.ServiceUnavailable);

        var error = await Assert.ThrowsAsync<ServicioNoDisponibleException>(
            () => adaptador.ObtenerProveedoresAsync(new FiltroProveedores()));

        Assert.Equal("Proveedores", error.Servicio);
    }
}
