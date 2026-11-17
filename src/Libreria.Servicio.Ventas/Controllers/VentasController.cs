using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Controllers.Comun;
using Libreria.Servicio.Ventas.Controllers.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly IVentaFacade _ventaFacade;
    private readonly IComprobanteVentaService? _comprobanteVentaService;

    public VentasController(
        IVentaFacade ventaFacade,
        IComprobanteVentaService? comprobanteVentaService = null)
    {
        _ventaFacade = ventaFacade;
        _comprobanteVentaService = comprobanteVentaService;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { mensaje = "Servicio de Ventas SBA operativo" });
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerListado(
        [FromQuery] string? estado,
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta,
        [FromQuery] OrdenVentas orden = OrdenVentas.Fecha,
        [FromQuery] bool descendente = true)
    {
        var filtro = new FiltroVentasInput
        {
            Estado = estado,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta,
            Orden = orden,
            Descendente = descendente
        };

        var ventas = await _ventaFacade.ObtenerListadoAsync(filtro);

        var respuesta = ventas.Select(v => new VentaListItemResponse(
            v.PublicId,
            v.FechaCreacion,
            v.ClienteNombre,
            v.ClienteCiNit,
            v.Total,
            v.GananciaTotal,
            v.Estado,
            v.CreadoPor,
            v.AnuladoPor,
            v.FechaAnulacion)).ToList();

        return Ok(respuesta);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> ObtenerDetalle(Guid publicId)
    {
        if (publicId == Guid.Empty)
        {
            return BadRequest(new { mensaje = "El identificador de la venta es inválido." });
        }

        var detalle = await _ventaFacade.ObtenerDetalleCompletoAsync(publicId);
        if (detalle is null)
        {
            return NotFound(new { mensaje = "La venta solicitada no fue encontrada." });
        }

        var lineas = detalle.Lineas.Select(l => new VentaLineaResponse(
            l.ProductoNombre,
            l.Cantidad,
            l.PrecioUnitario,
            l.Subtotal,
            l.Ganancia)).ToList();

        var respuesta = new VentaDetalleResponse(
            detalle.PublicId,
            detalle.FechaCreacion,
            detalle.FechaAnulacion,
            detalle.MotivoAnulacion,
            detalle.Observacion,
            detalle.Estado,
            detalle.ClienteNombre,
            detalle.ClienteCiNit,
            detalle.CreadoPor,
            detalle.AnuladoPor,
            detalle.ModificadoPor,
            detalle.FechaModificacion,
            detalle.Total,
            detalle.GananciaTotal,
            lineas);

        return Ok(respuesta);
    }

    [HttpGet("{publicId:guid}/comprobante")]
    public async Task<IActionResult> ObtenerComprobante(Guid publicId)
    {
        if (publicId == Guid.Empty)
        {
            return BadRequest(new { mensaje = "El identificador de la venta es inválido." });
        }

        if (_comprobanteVentaService is null)
        {
            return StatusCode(500, new { mensaje = "El servicio de comprobantes no está configurado." });
        }

        try
        {
            var comprobante = await _comprobanteVentaService.ObtenerAsync(publicId);
            if (comprobante is null)
            {
                return NotFound(new { mensaje = "La venta solicitada no fue encontrada para el comprobante." });
            }

            var detalles = comprobante.Detalles.Select(d => new DetalleComprobanteResponse(
                d.NombreProducto,
                d.Cantidad,
                d.PrecioUnitario,
                d.Importe)).ToList();

            var respuesta = new ComprobanteResponse(
                comprobante.PublicId,
                comprobante.Estado,
                comprobante.CiNitCliente,
                comprobante.RazonSocialCliente,
                detalles,
                comprobante.Total,
                comprobante.TotalEnLetras,
                comprobante.FechaVenta,
                comprobante.GeneradoEl,
                comprobante.GeneradoPor);

            return Ok(respuesta);
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { mensaje = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RegistrarVenta(
        [FromBody] RegistrarVentaInput input,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _ventaFacade.RegistrarVentaAsync(input, cancellationToken);
        return resultado.AActionResult(venta => Ok(venta));
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> CorregirDatos(
        Guid publicId,
        [FromBody] CorregirVentaInput input,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _ventaFacade.CorregirDatosAsync(
            publicId,
            input,
            cancellationToken);

        return resultado.AActionResult(() => Ok(new
        {
            exito = true,
            mensaje = "Los datos de la venta fueron corregidos exitosamente."
        }));
    }

    [HttpPost("{publicId:guid}/anular")]
    public async Task<IActionResult> AnularVenta(
        Guid publicId,
        [FromBody] AnularVentaRequest request,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _ventaFacade.AnularVentaAsync(
            publicId,
            request?.Motivo ?? string.Empty,
            cancellationToken);

        return resultado.AActionResult(() => Ok(new
        {
            exito = true,
            mensaje = "La venta fue anulada exitosamente y el stock ha sido restaurado."
        }));
    }
}
