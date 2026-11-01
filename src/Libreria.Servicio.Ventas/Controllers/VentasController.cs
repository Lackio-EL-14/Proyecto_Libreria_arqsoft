using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
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

    public VentasController(IVentaFacade ventaFacade)
    {
        _ventaFacade = ventaFacade;
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
        [FromQuery] DateOnly? fechaHasta)
    {
        var filtro = new FiltroVentasInput
        {
            Estado = estado,
            FechaDesde = fechaDesde,
            FechaHasta = fechaHasta
        };

        var ventas = await _ventaFacade.ObtenerListadoAsync(filtro);

        var respuesta = ventas.Select(v => new VentaListItemResponse(
            v.PublicId,
            v.FechaCreacion,
            v.ClienteNombre,
            v.ClienteCiNit,
            v.Total,
            v.Estado,
            v.CreadoPor,
            v.AnuladoPor,
            v.FechaAnulacion)).ToList();

        return Ok(respuesta);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> ObtenerDetalle(
        Guid publicId,
        [FromQuery] bool esAdministrador = false)
    {
        if (publicId == Guid.Empty)
        {
            return BadRequest(new { mensaje = "El identificador de la venta es inválido." });
        }

        var detalle = await _ventaFacade.ObtenerDetalleCompletoAsync(publicId, esAdministrador);
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
            detalle.Estado,
            detalle.ClienteNombre,
            detalle.ClienteCiNit,
            detalle.CreadoPor,
            detalle.AnuladoPor,
            detalle.Total,
            detalle.GananciaTotal,
            lineas);

        return Ok(respuesta);
    }

    [HttpPost("{publicId:guid}/anular")]
    public async Task<IActionResult> AnularVenta(Guid publicId, [FromBody] AnularVentaRequest request)
    {
        if (publicId == Guid.Empty)
        {
            return BadRequest(new { mensaje = "El identificador de la venta es inválido." });
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Motivo))
        {
            return BadRequest(new { mensaje = "Debe ingresar el motivo de la anulación." });
        }

        var motivo = request.Motivo.Trim();
        if (motivo.Length < 10 || motivo.Length > 250)
        {
            return BadRequest(new { mensaje = "El motivo de la anulación debe tener entre 10 y 250 caracteres." });
        }

        try
        {
            await _ventaFacade.AnularVentaAsync(publicId, motivo);
            return Ok(new { exito = true, mensaje = "La venta fue anulada exitosamente y el stock ha sido restaurado." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { mensaje = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { mensaje = "Ocurrió un error inesperado al procesar la anulación de la venta." });
        }
    }
}
