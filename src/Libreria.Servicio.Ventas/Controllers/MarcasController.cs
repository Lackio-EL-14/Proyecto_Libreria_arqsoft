using Libreria.Servicio.Ventas.Application.Factories;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Validators;
using Libreria.Servicio.Ventas.Controllers.Dtos;
using Libreria.Servicio.Ventas.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/marcas")]
public class MarcasController : ControllerBase
{
    private readonly ICrudRepository<Marca> _repositorio;
    private readonly MarcaValidator _validador;

    public MarcasController(
        CrudRepositoryFactory<Marca> fabrica,
        MarcaValidator validador)
    {
        _repositorio = fabrica.CrearRepositorio();
        _validador = validador;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? nombre)
    {
        var marcas = await _repositorio.ObtenerActivasAsync(nombre?.Trim());

        var respuesta = new List<MarcaResponse>(marcas.Count);
        foreach (var marca in marcas)
        {
            var tieneProductos = await _repositorio.TieneRelacionesAsync(marca.PublicId);
            respuesta.Add(Mapear(marca, tieneProductos));
        }

        return Ok(respuesta);
    }

    [HttpGet("paises")]
    public IActionResult ObtenerPaises()
    {
        return Ok(PaisesMarca.Todos);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> ObtenerPorId(Guid publicId, [FromQuery] bool estadoEsperado = true)
    {
        var marca = await _repositorio.ObtenerPorPublicIdAsync(publicId, estadoEsperado);
        if (marca is null)
        {
            return NotFound();
        }

        var tieneProductos = await _repositorio.TieneRelacionesAsync(publicId);
        return Ok(Mapear(marca, tieneProductos));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] MarcaInput input)
    {
        _validador.Normalizar(input);
        var errores = await _validador.ValidarAsync(input);
        if (errores.Count > 0)
        {
            return BadRequest(errores);
        }

        await _repositorio.CrearAsync(new Marca
        {
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            PaisOrigen = input.PaisOrigen,
            SitioWeb = input.SitioWeb
        });

        return StatusCode(201);
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(Guid publicId, [FromBody] MarcaInput input)
    {
        var original = await _repositorio.ObtenerPorPublicIdAsync(publicId, true);
        if (original is null)
        {
            return NotFound();
        }

        _validador.Normalizar(input);
        var errores = await _validador.ValidarAsync(input, publicId);
        if (errores.Count > 0)
        {
            return BadRequest(errores);
        }

        var actualizada = await _repositorio.ActualizarAsync(new Marca
        {
            PublicId = publicId,
            Nombre = input.Nombre,
            Descripcion = input.Descripcion,
            PaisOrigen = input.PaisOrigen,
            SitioWeb = input.SitioWeb
        });

        return actualizada ? NoContent() : NotFound();
    }

    [HttpPatch("{publicId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid publicId, [FromQuery] bool nuevoEstado)
    {
        var cambiado = await _repositorio.CambiarEstadoAsync(publicId, nuevoEstado);
        return cambiado ? NoContent() : NotFound();
    }

    private static MarcaResponse Mapear(Marca marca, bool tieneProductosActivos)
    {
        return new MarcaResponse(
            marca.PublicId,
            marca.Nombre,
            marca.Descripcion,
            marca.PaisOrigen,
            marca.SitioWeb,
            marca.Estado,
            marca.UsuarioCreacionId,
            marca.UsuarioModificacionId,
            tieneProductosActivos);
    }
}
