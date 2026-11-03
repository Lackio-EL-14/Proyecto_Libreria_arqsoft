
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/ventas/busquedas")]
public sealed class VentasBusquedasController : ControllerBase
{
    private readonly IConsultaVentaService _consulta;

    public VentasBusquedasController(IConsultaVentaService consulta)
    {
        _consulta = consulta;
    }

    [HttpGet("productos")]
    public async Task<IActionResult> BuscarProductos(
        [FromQuery] string? nombre)
    {
        var productos = await _consulta.BuscarProductosAsync(nombre);
        return Ok(productos);
    }

    [HttpGet("clientes")]
    public async Task<IActionResult> BuscarClientes(
        [FromQuery] string? ciNit)
    {
        var clientes = await _consulta.BuscarClientesAsync(ciNit);
        return Ok(clientes);
    }
}
