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

public class VentasControllerAnulacionTests
{
    [Fact]
    public async Task AnularVenta_ConIdVacio_RetornaBadRequest()
    {
        // Arrange
        var fakeFacade = new FakeVentaFacade();
        var controller = new VentasController(fakeFacade);
        var request = new AnularVentaRequest("Motivo de anulación válido");

        // Act
        var result = await controller.AnularVenta(Guid.Empty, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Corto")] // Menor a 10 caracteres
    public async Task AnularVenta_ConMotivoInvalido_RetornaBadRequest(string motivoInvalido)
    {
        // Arrange
        var fakeFacade = new FakeVentaFacade();
        var controller = new VentasController(fakeFacade);
        var request = new AnularVentaRequest(motivoInvalido);

        // Act
        var result = await controller.AnularVenta(Guid.NewGuid(), request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AnularVenta_ConVentaInexistente_RetornaNotFound()
    {
        // Arrange
        var fakeFacade = new FakeVentaFacade
        {
            OnAnular = (id, motivo) => throw new KeyNotFoundException("La venta especificada no existe.")
        };
        var controller = new VentasController(fakeFacade);
        var request = new AnularVentaRequest("Cliente devolvió los productos");

        // Act
        var result = await controller.AnularVenta(Guid.NewGuid(), request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task AnularVenta_CuandoVentaYaEstaAnulada_RetornaConflict()
    {
        // Arrange
        var fakeFacade = new FakeVentaFacade
        {
            OnAnular = (id, motivo) => throw new InvalidOperationException("La venta ya se encuentra anulada.")
        };
        var controller = new VentasController(fakeFacade);
        var request = new AnularVentaRequest("Cliente devolvió los productos");

        // Act
        var result = await controller.AnularVenta(Guid.NewGuid(), request);

        // Assert
        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.NotNull(conflict.Value);
    }

    [Fact]
    public async Task AnularVenta_ValidaYExistente_RetornaOk()
    {
        // Arrange
        var ventaId = Guid.NewGuid();
        Guid? idAnulado = null;
        string? motivoRegistrado = null;

        var fakeFacade = new FakeVentaFacade
        {
            OnAnular = (id, motivo) =>
            {
                idAnulado = id;
                motivoRegistrado = motivo;
                return Task.CompletedTask;
            }
        };

        var controller = new VentasController(fakeFacade);
        var request = new AnularVentaRequest("Devolución completa de productos en caja");

        // Act
        var result = await controller.AnularVenta(ventaId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Equal(ventaId, idAnulado);
        Assert.Equal("Devolución completa de productos en caja", motivoRegistrado);
    }

    private class FakeVentaFacade : IVentaFacade
    {
        public Func<Guid, string, Task>? OnAnular { get; set; }

        public Task AnularVentaAsync(Guid publicId, string motivoAnulacion)
        {
            if (OnAnular != null)
            {
                return OnAnular(publicId, motivoAnulacion);
            }
            return Task.CompletedTask;
        }

        public Task<VentaRegistradaResult> RegistrarVentaAsync(RegistrarVentaInput input) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(FiltroVentasInput filtro) =>
            throw new NotImplementedException();

        public Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(Guid publicId, bool esAdministrador) =>
            throw new NotImplementedException();
    }
}
