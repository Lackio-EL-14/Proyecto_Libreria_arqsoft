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
public class CategoriasController : ControllerBase
{
    private readonly ICrudRepository<Categoria> _categoriaRepository;
    private readonly CategoriaValidator _validator;

    public CategoriasController(
        ICrudRepository<Categoria> categoriaRepository,
        CategoriaValidator validator)
    {
        _categoriaRepository = categoriaRepository;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodas([FromQuery] string? busqueda = null)
    {
        var listado = await _categoriaRepository.ObtenerActivasAsync(busqueda);
        return Ok(listado);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid publicId, [FromQuery] bool estadoEsperado = true)
    {
        var categoria = await _categoriaRepository.ObtenerPorPublicIdAsync(publicId, estadoEsperado);
        if (categoria == null) return NotFound();
        return Ok(categoria);
    }

    [HttpGet("{publicId:guid}/tiene-relaciones")]
    public async Task<IActionResult> TieneRelaciones(Guid publicId)
    {
        var tiene = await _categoriaRepository.TieneRelacionesAsync(publicId);
        return Ok(tiene);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CategoriaInput input)
    {
        _validator.Normalizar(input);
        var errores = await _validator.ValidarCreacionAsync(input);
        if (errores.Count > 0) return BadRequest(errores);

        var categoria = new Categoria
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            Ubicacion = input.Ubicacion
        };
        
        await _categoriaRepository.CrearAsync(categoria);
        return Ok();
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(Guid publicId, [FromBody] CategoriaInput input)
    {
        var categoriaOriginal = await _categoriaRepository.ObtenerPorPublicIdAsync(publicId, true);
        if (categoriaOriginal == null) return NotFound();

        input.Codigo = categoriaOriginal.Codigo;
        
        _validator.Normalizar(input);
        var errores = await _validator.ValidarEdicionAsync(input, publicId);
        if (errores.Count > 0) return BadRequest(errores);

        var actualizada = await _categoriaRepository.ActualizarAsync(new Categoria
        {
            PublicId = publicId,
            Codigo = categoriaOriginal.Codigo,
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            Ubicacion = input.Ubicacion
        });

        if (!actualizada) return NotFound();
        return Ok();
    }

    [HttpPatch("{publicId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid publicId, [FromQuery] bool nuevoEstado)
    {
        var resultado = await _categoriaRepository.CambiarEstadoAsync(publicId, nuevoEstado);
        if (!resultado) return NotFound();
        return Ok();
    }
}
