using Libreria.Servicio.Productos.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Libreria.Servicio.Productos.Application.Models.Categorias;
using Libreria.Servicio.Productos.Application.UseCases.Categorias;
using Libreria.Servicio.Productos.Domain;
using Libreria.Servicio.Productos.Presentation.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Productos.Presentation.Controllers;

[ApiController]
// US-126: solo el Administrador gestiona el inventario.
[Authorize(Policy = SeguridadJwtExtensions.SoloAdministrador)]
[Route("api/categorias")]
public sealed class CategoriasController : ControllerBase
{
    private readonly ListarCategoriasUseCase _listar;
    private readonly ObtenerCategoriaUseCase _obtener;
    private readonly CrearCategoriaUseCase _crear;
    private readonly ActualizarCategoriaUseCase _actualizar;
    private readonly CambiarEstadoCategoriaUseCase _cambiarEstado;

    public CategoriasController(
        ListarCategoriasUseCase listar,
        ObtenerCategoriaUseCase obtener,
        CrearCategoriaUseCase crear,
        ActualizarCategoriaUseCase actualizar,
        CambiarEstadoCategoriaUseCase cambiarEstado)
    {
        _listar = listar;
        _obtener = obtener;
        _crear = crear;
        _actualizar = actualizar;
        _cambiarEstado = cambiarEstado;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda)
    {
        var resultado = await _listar.EjecutarAsync(busqueda);
        return Ok(resultado);
    }

    [HttpGet("ubicaciones")]
    public IActionResult ObtenerUbicaciones()
    {
        return Ok(UbicacionesCategoria.Todas);
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
        [FromBody] CategoriaInput input)
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
        [FromBody] CategoriaInput input)
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