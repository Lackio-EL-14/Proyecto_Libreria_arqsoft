using Libreria.Servicio.Ventas.Application.Factories;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Validators;
using Libreria.Servicio.Ventas.Controllers.Dtos;
using Libreria.Servicio.Ventas.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly ICrudRepository<Categoria> _repositorio;
    private readonly CategoriaValidator _validador;

    // Factory Method: el controlador no conoce el repositorio concreto,
    // lo pide a la fábrica registrada (igual que el ejemplo del docente).
    public CategoriasController(
        CrudRepositoryFactory<Categoria> fabrica,
        CategoriaValidator validador)
    {
        _repositorio = fabrica.CrearRepositorio();
        _validador = validador;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? busqueda)
    {
        var categorias = await _repositorio.ObtenerActivasAsync(busqueda);

        var respuesta = new List<CategoriaResponse>(categorias.Count);
        foreach (var categoria in categorias)
        {
            var tieneProductos = await _repositorio.TieneRelacionesAsync(categoria.PublicId);
            respuesta.Add(Mapear(categoria, tieneProductos));
        }

        return Ok(respuesta);
    }

    [HttpGet("ubicaciones")]
    public IActionResult ObtenerUbicaciones()
    {
        return Ok(UbicacionesCategoria.Todas);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid publicId, [FromQuery] bool estadoEsperado = true)
    {
        var categoria = await _repositorio.ObtenerPorPublicIdAsync(publicId, estadoEsperado);
        if (categoria is null)
        {
            return NotFound();
        }

        var tieneProductos = await _repositorio.TieneRelacionesAsync(publicId);
        return Ok(Mapear(categoria, tieneProductos));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CategoriaInput input)
    {
        _validador.Normalizar(input);
        var errores = await _validador.ValidarCreacionAsync(input);
        if (errores.Count > 0)
        {
            return BadRequest(errores);
        }

        await _repositorio.CrearAsync(new Categoria
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            Ubicacion = input.Ubicacion
        });

        return StatusCode(201);
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(Guid publicId, [FromBody] CategoriaInput input)
    {
        var original = await _repositorio.ObtenerPorPublicIdAsync(publicId, true);
        if (original is null)
        {
            return NotFound();
        }

        // El código es autogenerado y de solo lectura: siempre se conserva el original.
        input.Codigo = original.Codigo;

        _validador.Normalizar(input);
        var errores = await _validador.ValidarEdicionAsync(input, publicId);
        if (errores.Count > 0)
        {
            return BadRequest(errores);
        }

        var actualizada = await _repositorio.ActualizarAsync(new Categoria
        {
            PublicId = publicId,
            Codigo = original.Codigo,
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            Ubicacion = input.Ubicacion
        });

        return actualizada ? NoContent() : NotFound();
    }

    [HttpPatch("{publicId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid publicId, [FromQuery] bool nuevoEstado)
    {
        var cambiado = await _repositorio.CambiarEstadoAsync(publicId, nuevoEstado);
        return cambiado ? NoContent() : NotFound();
    }

    private static CategoriaResponse Mapear(Categoria categoria, bool tieneProductosActivos)
    {
        return new CategoriaResponse(
            categoria.PublicId,
            categoria.Codigo,
            categoria.Nombre,
            categoria.Descripcion,
            categoria.Ubicacion,
            categoria.Estado,
            categoria.UsuarioCreacionId,
            categoria.FechaCreacion,
            categoria.UsuarioModificacionId,
            categoria.FechaModificacion,
            tieneProductosActivos);
    }
}
