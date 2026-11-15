using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Comun;
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
        var badRequest = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
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
        var badRequest = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, badRequest.StatusCode);
    }

    [Fact]
    public async Task AnularVenta_ConVentaInexistente_RetornaNotFound()
    {
        // Arrange
        var fakeFacade = new FakeVentaFacade
        {
            OnAnular = (id, motivo) => Task.FromResult(Result.NoEncontrado("La venta especificada no existe."))
        };
        var controller = new VentasController(fakeFacade);
        var request = new AnularVentaRequest("Cliente devolvió los productos");

        // Act
        var result = await controller.AnularVenta(Guid.NewGuid(), request);

        // Assert
        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, notFound.StatusCode);
    }

    [Fact]
    public async Task AnularVenta_CuandoVentaYaEstaAnulada_RetornaConflict()
    {
        // Arrange
        var fakeFacade = new FakeVentaFacade
        {
            OnAnular = (id, motivo) => Task.FromResult(Result.Conflicto("Venta", "La venta ya se encuentra anulada."))
        };
        var controller = new VentasController(fakeFacade);
        var request = new AnularVentaRequest("Cliente devolvió los productos");

        // Act
        var result = await controller.AnularVenta(Guid.NewGuid(), request);

        // Assert
        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
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
                return Task.FromResult(Result.Exito());
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
        public Func<Guid, string, Task<Result>>? OnAnular { get; set; }

        public Task<Result> AnularVentaAsync(
            Guid publicId,
            string motivoAnulacion,
            CancellationToken cancellationToken = default)
        {
            if (OnAnular != null)
            {
                return OnAnular(publicId, motivoAnulacion);
            }

            if (publicId == Guid.Empty)
            {
                return Task.FromResult(Result.Validacion(
                    "PublicId",
                    "El identificador de la venta es inválido."));
            }

            var motivo = motivoAnulacion?.Trim();
            if (motivo is null || motivo.Length is < 10 or > 250)
            {
                return Task.FromResult(Result.Validacion(
                    "Motivo",
                    "El motivo de la anulación debe tener entre 10 y 250 caracteres."));
            }

            return Task.FromResult(Result.Exito());
        }

        public Task<Result<VentaRegistradaResult>> RegistrarVentaAsync(
            RegistrarVentaInput input,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(FiltroVentasInput filtro) =>
            throw new NotImplementedException();

        public Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(Guid publicId, bool esAdministrador) =>
            throw new NotImplementedException();
    }
}
