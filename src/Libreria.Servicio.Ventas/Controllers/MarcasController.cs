using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Validators;
using Libreria.Servicio.Ventas.Domain;
using System;
using System.Threading.Tasks;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MarcasController : ControllerBase
{
    private readonly ICrudRepository<Marca> _marcaRepository;
    private readonly MarcaValidator _validator;

    public MarcasController(
        ICrudRepository<Marca> marcaRepository,
        MarcaValidator validator)
    {
        _marcaRepository = marcaRepository;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas([FromQuery] string? busqueda = null)
    {
        var listado = await _marcaRepository.ObtenerActivasAsync(busqueda);
        return Ok(listado);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid publicId, [FromQuery] bool estadoEsperado = true)
    {
        var marca = await _marcaRepository.ObtenerPorPublicIdAsync(publicId, estadoEsperado);
        if (marca == null) return NotFound();
        return Ok(marca);
    }

    [HttpGet("{publicId:guid}/tiene-relaciones")]
    public async Task<IActionResult> TieneRelaciones(Guid publicId)
    {
        var tiene = await _marcaRepository.TieneRelacionesAsync(publicId);
        return Ok(tiene);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] MarcaInput input)
    {
        _validator.Normalizar(input);
        var errores = await _validator.ValidarAsync(input);
        if (errores.Count > 0) return BadRequest(errores);

        var marca = new Marca
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            PaisOrigen = input.PaisOrigen,
            SitioWeb = input.SitioWeb
        };
        
        await _marcaRepository.CrearAsync(marca);
        return Ok();
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(Guid publicId, [FromBody] MarcaInput input)
    {
        var marcaOriginal = await _marcaRepository.ObtenerPorPublicIdAsync(publicId, true);
        if (marcaOriginal == null) return NotFound();

        _validator.Normalizar(input);
        var errores = await _validator.ValidarAsync(input, publicId);
        if (errores.Count > 0) return BadRequest(errores);

        var actualizada = await _marcaRepository.ActualizarAsync(new Marca
        {
            PublicId = publicId,
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            PaisOrigen = input.PaisOrigen,
            SitioWeb = input.SitioWeb
        });

        if (!actualizada) return NotFound();
        return Ok();
    }

    [HttpPatch("{publicId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid publicId, [FromQuery] bool nuevoEstado)
    {
        var resultado = await _marcaRepository.CambiarEstadoAsync(publicId, nuevoEstado);
        if (!resultado) return NotFound();
        return Ok();
    }
}
