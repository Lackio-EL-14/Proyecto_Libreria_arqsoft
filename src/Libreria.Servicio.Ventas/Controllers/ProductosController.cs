using Libreria.Servicio.Ventas.Application.Factories;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Services;
using Libreria.Servicio.Ventas.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/productos")]
public class ProductosController : ControllerBase
{
    private readonly IProductoService _servicio;
    private readonly ICrudRepository<Producto> _repositorio;

    public ProductosController(
        IProductoService servicio,
        CrudRepositoryFactory<Producto> fabrica)
    {
        _servicio = servicio;
        _repositorio = fabrica.CrearRepositorio();
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int? categoriaId,
        [FromQuery] int? marcaId)
    {
        var listado = await _servicio.ObtenerListadoAsync(busqueda, categoriaId, marcaId, _repositorio);
        return Ok(listado);
    }

    [HttpGet("formulario")]
    public IActionResult ObtenerFormulario()
    {
        return Ok(_servicio.ObtenerFormulario());
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> ObtenerDetalle(Guid publicId)
    {
        var detalle = await _servicio.ObtenerDetalleAsync(publicId, _repositorio);
        return detalle is null ? NotFound() : Ok(detalle);
    }

    [HttpGet("{publicId:guid}/edicion")]
    public async Task<IActionResult> ObtenerEdicion(Guid publicId)
    {
        var edicion = await _servicio.ObtenerEdicionAsync(publicId, _repositorio);
        return edicion is null ? NotFound() : Ok(edicion);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] ProductoInput input)
    {
        var resultado = await _servicio.RegistrarAsync(input, _repositorio);
        if (!resultado.Exitoso)
        {
            return BadRequest(resultado.Errores);
        }

        return StatusCode(201);
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(Guid publicId, [FromBody] ProductoInput input)
    {
        var resultado = await _servicio.ActualizarAsync(publicId, input, _repositorio);
        if (resultado.NoEncontrado)
        {
            return NotFound();
        }

        if (!resultado.Exitoso)
        {
            return BadRequest(resultado.Errores);
        }

        return NoContent();
    }

    [HttpPatch("{publicId:guid}/baja")]
    public async Task<IActionResult> DarDeBaja(Guid publicId)
    {
        var dadoDeBaja = await _servicio.DarDeBajaAsync(publicId, _repositorio);
        return dadoDeBaja ? NoContent() : NotFound();
    }
}
