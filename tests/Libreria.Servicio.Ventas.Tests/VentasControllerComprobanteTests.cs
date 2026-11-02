using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Controllers;
using Libreria.Servicio.Ventas.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Libreria.Servicio.Ventas.Tests;

public class VentasControllerComprobanteTests
{
    [Fact]
    public async Task ObtenerComprobante_ConIdVacio_RetornaBadRequest()
    {
        var fakeService = new FakeComprobanteVentaService();
        var controller = new VentasController(null!, fakeService);

        var result = await controller.ObtenerComprobante(Guid.Empty);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task ObtenerComprobante_CuandoNoExiste_RetornaNotFound()
    {
        var fakeService = new FakeComprobanteVentaService { ComprobanteRetorno = null };
        var controller = new VentasController(null!, fakeService);

        var result = await controller.ObtenerComprobante(Guid.NewGuid());

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(notFound.Value);
    }

    [Fact]
    public async Task ObtenerComprobante_CuandoUsuarioNoAutenticado_RetornaUnauthorized()
    {
        var fakeService = new FakeComprobanteVentaService
        {
            LanzarInvalidOperation = true
        };
        var controller = new VentasController(null!, fakeService);

        var result = await controller.ObtenerComprobante(Guid.NewGuid());

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.NotNull(unauthorized.Value);
    }

    [Fact]
    public async Task ObtenerComprobante_CuandoExiste_RetornaOkConComprobanteCompleto()
    {
        var publicId = Guid.NewGuid();
        var comprobante = new ComprobanteVenta
        {
            PublicId = publicId,
            Estado = "Activa",
            CiNitCliente = "12345678",
            RazonSocialCliente = "Cliente de Prueba",
            Detalles = new List<DetalleComprobanteVenta>
            {
                new("Libro Limpio", 2, 50m, 100m)
            },
            Total = 100m,
            TotalEnLetras = "Son: cien con 00/100 bolivianos",
            FechaVenta = DateTimeOffset.UtcNow.AddHours(-1),
            GeneradoEl = DateTimeOffset.UtcNow,
            GeneradoPor = "admin"
        };

        var fakeService = new FakeComprobanteVentaService { ComprobanteRetorno = comprobante };
        var controller = new VentasController(null!, fakeService);

        var result = await controller.ObtenerComprobante(publicId);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ComprobanteResponse>(okResult.Value);

        Assert.Equal(publicId, response.PublicId);
        Assert.Equal("Activa", response.Estado);
        Assert.Equal("12345678", response.CiNitCliente);
        Assert.Equal("Cliente de Prueba", response.RazonSocialCliente);
        Assert.Equal(100m, response.Total);
        Assert.Equal("Son: cien con 00/100 bolivianos", response.TotalEnLetras);
        Assert.Equal("admin", response.GeneradoPor);
        Assert.Single(response.Detalles);
        Assert.Equal("Libro Limpio", response.Detalles[0].NombreProducto);
        Assert.Equal(2, response.Detalles[0].Cantidad);
        Assert.Equal(50m, response.Detalles[0].PrecioUnitario);
        Assert.Equal(100m, response.Detalles[0].Importe);
    }

    private sealed class FakeComprobanteVentaService : IComprobanteVentaService
    {
        public ComprobanteVenta? ComprobanteRetorno { get; set; }
        public bool LanzarInvalidOperation { get; set; }

        public Task<ComprobanteVenta?> ObtenerAsync(Guid publicId)
        {
            if (LanzarInvalidOperation)
            {
                throw new InvalidOperationException("Debe iniciar sesión para consultar el comprobante.");
            }

            return Task.FromResult(ComprobanteRetorno);
        }
    }
}
