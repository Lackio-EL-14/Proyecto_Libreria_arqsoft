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

public class VentasControllerListadoDetalleTests
{
    [Fact]
    public async Task ObtenerListado_RetornaOkConListaDeVentas()
    {
        var ventasEsperadas = new List<VentaListItemModel>
        {
            new()
            {
                PublicId = Guid.NewGuid(),
                FechaCreacion = DateTime.UtcNow,
                ClienteNombre = "Universidad Mayor",
                ClienteCiNit = "9999999",
                Total = 135.00m,
                Estado = "Activa",
                CreadoPor = "admin"
            }
        };

        var fakeFacade = new FakeVentaFacade
        {
            OnObtenerListado = filtro => Task.FromResult<IReadOnlyList<VentaListItemModel>>(ventasEsperadas)
        };

        var controller = new VentasController(fakeFacade);

        var result = await controller.ObtenerListado(null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<VentaListItemResponse>>(okResult.Value);
        Assert.Single(items);
        Assert.Equal("Universidad Mayor", items[0].ClienteNombre);
        Assert.Equal(135.00m, items[0].Total);
    }

    [Fact]
    public async Task ObtenerDetalle_ConIdVacio_RetornaBadRequest()
    {
        var fakeFacade = new FakeVentaFacade();
        var controller = new VentasController(fakeFacade);

        var result = await controller.ObtenerDetalle(Guid.Empty, true);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ObtenerDetalle_CuandoVentaNoExiste_RetornaNotFound()
    {
        var fakeFacade = new FakeVentaFacade
        {
            OnObtenerDetalle = (id, esAdmin) => Task.FromResult<VentaDetalleCompletoModel?>(null)
        };

        var controller = new VentasController(fakeFacade);

        var result = await controller.ObtenerDetalle(Guid.NewGuid(), true);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ObtenerDetalle_ComoAdministrador_IncluyeGanancia()
    {
        var publicId = Guid.NewGuid();
        var detalleModel = new VentaDetalleCompletoModel
        {
            PublicId = publicId,
            ClienteNombre = "Juan Perez",
            ClienteCiNit = "1234567",
            Estado = "Activa",
            Total = 100m,
            GananciaTotal = 25m,
            Lineas = new List<VentaLineaDetalleModel>
            {
                new()
                {
                    ProductoNombre = "Libro de Álgebra",
                    Cantidad = 1,
                    PrecioUnitario = 100m,
                    Subtotal = 100m,
                    Ganancia = 25m
                }
            }
        };

        var fakeFacade = new FakeVentaFacade
        {
            OnObtenerDetalle = (id, esAdmin) => Task.FromResult<VentaDetalleCompletoModel?>(detalleModel)
        };

        var controller = new VentasController(fakeFacade);

        var result = await controller.ObtenerDetalle(publicId, esAdministrador: true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var detalle = Assert.IsType<VentaDetalleResponse>(okResult.Value);
        Assert.Equal(25m, detalle.GananciaTotal);
        Assert.Equal(25m, detalle.Lineas[0].Ganancia);
    }

    [Fact]
    public async Task ObtenerDetalle_ComoVendedor_NoIncluyeGanancia()
    {
        var publicId = Guid.NewGuid();
        var detalleModel = new VentaDetalleCompletoModel
        {
            PublicId = publicId,
            ClienteNombre = "Juan Perez",
            ClienteCiNit = "1234567",
            Estado = "Activa",
            Total = 100m,
            GananciaTotal = null,
            Lineas = new List<VentaLineaDetalleModel>
            {
                new()
                {
                    ProductoNombre = "Libro de Álgebra",
                    Cantidad = 1,
                    PrecioUnitario = 100m,
                    Subtotal = 100m,
                    Ganancia = null
                }
            }
        };

        var fakeFacade = new FakeVentaFacade
        {
            OnObtenerDetalle = (id, esAdmin) => Task.FromResult<VentaDetalleCompletoModel?>(detalleModel)
        };

        var controller = new VentasController(fakeFacade);

        var result = await controller.ObtenerDetalle(publicId, esAdministrador: false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var detalle = Assert.IsType<VentaDetalleResponse>(okResult.Value);
        Assert.Null(detalle.GananciaTotal);
        Assert.Null(detalle.Lineas[0].Ganancia);
    }

    private class FakeVentaFacade : IVentaFacade
    {
        public Func<FiltroVentasInput, Task<IReadOnlyList<VentaListItemModel>>>? OnObtenerListado { get; set; }
        public Func<Guid, bool, Task<VentaDetalleCompletoModel?>>? OnObtenerDetalle { get; set; }

        public Task<IReadOnlyList<VentaListItemModel>> ObtenerListadoAsync(FiltroVentasInput filtro)
        {
            if (OnObtenerListado != null)
            {
                return OnObtenerListado(filtro);
            }
            return Task.FromResult<IReadOnlyList<VentaListItemModel>>(new List<VentaListItemModel>());
        }

        public Task<VentaDetalleCompletoModel?> ObtenerDetalleCompletoAsync(Guid publicId, bool esAdministrador)
        {
            if (OnObtenerDetalle != null)
            {
                return OnObtenerDetalle(publicId, esAdministrador);
            }
            return Task.FromResult<VentaDetalleCompletoModel?>(null);
        }

        public Task AnularVentaAsync(Guid publicId, string motivoAnulacion) => Task.CompletedTask;

        public Task<VentaRegistradaResult> RegistrarVentaAsync(RegistrarVentaInput input) =>
            throw new NotImplementedException();
    }
}
