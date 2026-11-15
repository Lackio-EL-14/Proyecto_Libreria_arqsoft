using Libreria.Servicio.Productos.Application.Models.Marcas;
using Libreria.Servicio.Productos.Application.UseCases.Marcas;
using Libreria.Servicio.Productos.Domain;
using Libreria.Servicio.Productos.Presentation.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Productos.Presentation.Controllers;

[ApiController]
[Route("api/marcas")]
public sealed class MarcasController : ControllerBase
{
    private readonly ListarMarcasUseCase _listar;
    private readonly ObtenerMarcaUseCase _obtener;
    private readonly CrearMarcaUseCase _crear;
    private readonly ActualizarMarcaUseCase _actualizar;
    private readonly CambiarEstadoMarcaUseCase _cambiarEstado;

    public MarcasController(
        ListarMarcasUseCase listar,
        ObtenerMarcaUseCase obtener,
        CrearMarcaUseCase crear,
        ActualizarMarcaUseCase actualizar,
        CambiarEstadoMarcaUseCase cambiarEstado)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _cambiarEstado = cambiarEstado;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? nombre)
    {
        var resultado = await _listar.EjecutarAsync(nombre);
        return Ok(resultado);
    }

    [HttpGet("paises")]
    public IActionResult ObtenerPaises()
    {
        return Ok(PaisesMarca.Todos);
    }

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> Obtener(
        Guid publicId,
        [FromQuery] bool estadoEsperado = true)
    {
        var resultado = await _obtener.EjecutarAsync(
            publicId,
            estadoEsperado);

        return resultado.AActionResult(Ok);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] MarcaInput input)
    {
        var resultado = await _crear.EjecutarAsync(input);

        return resultado.AActionResult(publicId =>
            CreatedAtAction(
                nameof(Obtener),
                new
                {
                    publicId,
                    estadoEsperado = true
                },
                new { publicId }));
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(
        Guid publicId,
        [FromBody] MarcaInput input)
    {
        var resultado = await _actualizar.EjecutarAsync(
            publicId,
            input);

        return resultado.AActionResult();
    }

    [HttpPatch("{publicId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(
        Guid publicId,
        [FromQuery] bool nuevoEstado)
    {
        var resultado = await _cambiarEstado.EjecutarAsync(
            publicId,
            nuevoEstado);

        return resultado.AActionResult();
    }
}