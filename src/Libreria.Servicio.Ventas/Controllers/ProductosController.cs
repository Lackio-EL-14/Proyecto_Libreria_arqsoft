using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Services;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Domain;
using System;
using System.Threading.Tasks;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;
    private readonly ICrudRepository<Producto> _productoRepository;

    public ProductosController(
        IProductoService productoService,
        ICrudRepository<Producto> productoRepository)
    {
        _productoService = productoService;
        _productoRepository = productoRepository;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerListado([FromQuery] string? busqueda, [FromQuery] int? categoriaId, [FromQuery] int? marcaId)
    {
        var listado = await _productoService.ObtenerListadoAsync(busqueda, categoriaId, marcaId, _productoRepository);
        return Ok(listado);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> ObtenerDetalle(Guid id)
    {
        var detalle = await _productoService.ObtenerDetalleAsync(id, _productoRepository);
        if (detalle == null) return NotFound();
        return Ok(detalle);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] ProductoInput input)
    {
        var resultado = await _productoService.RegistrarAsync(input, _productoRepository);
        if (!resultado.Exitoso) return BadRequest(resultado.Errores);
        return Ok();
    }
}
