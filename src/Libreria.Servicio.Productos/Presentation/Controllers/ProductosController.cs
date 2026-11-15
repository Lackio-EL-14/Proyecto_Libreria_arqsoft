using Libreria.Servicio.Productos.Application.Models.Productos;
using Libreria.Servicio.Productos.Application.UseCases.Productos;
using Libreria.Servicio.Productos.Presentation.Comun;
using Libreria.Servicio.Productos.Presentation.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Productos.Presentation.Controllers;

[ApiController]
[Route("api/productos")]
public sealed class ProductosController : ControllerBase
{
    private readonly ObtenerListadoProductosUseCase _listar;
    private readonly ObtenerFormularioProductoUseCase _formulario;
    private readonly ObtenerDetalleProductoUseCase _obtenerDetalle;
    private readonly ObtenerProductoEdicionUseCase _obtenerEdicion;
    private readonly CrearProductoUseCase _crear;
    private readonly ActualizarProductoUseCase _actualizar;
    private readonly CambiarEstadoProductoUseCase _cambiarEstado;

    private readonly BuscarProductosUseCase _buscar;
    private readonly DescontarStockUseCase _descontarStock;
    private readonly RestaurarStockUseCase _restaurarStock;

    public ProductosController(
        ObtenerListadoProductosUseCase listar,
        ObtenerFormularioProductoUseCase formulario,
        ObtenerDetalleProductoUseCase obtenerDetalle,
        ObtenerProductoEdicionUseCase obtenerEdicion,
        CrearProductoUseCase crear,
        ActualizarProductoUseCase actualizar,
        CambiarEstadoProductoUseCase cambiarEstado,
        BuscarProductosUseCase buscar,
        DescontarStockUseCase descontarStock,
        RestaurarStockUseCase restaurarStock)
    {
        _listar = listar;
        _formulario = formulario;
        _obtenerDetalle = obtenerDetalle;
        _obtenerEdicion = obtenerEdicion;
        _crear = crear;
        _actualizar = actualizar;
        _cambiarEstado = cambiarEstado;

        _buscar = buscar;
        _descontarStock = descontarStock;
        _restaurarStock = restaurarStock;
    }

    // Front Productos: listado + filtros.
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] Guid? categoriaPublicId,
        [FromQuery] Guid? marcaPublicId)
    {
        var resultado = await _listar.EjecutarAsync(
            busqueda,
            categoriaPublicId,
            marcaPublicId);

        return Ok(resultado);
    }

    [HttpGet("formulario")]
    public async Task<IActionResult> ObtenerFormulario()
    {
        var resultado = await _formulario.EjecutarAsync();
        return Ok(resultado);
    }

    // También es el contrato que consume Ventas.
    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> Obtener(Guid publicId)
    {
        var resultado =
            await _obtenerDetalle.EjecutarAsync(publicId);

        return resultado.AActionResult(Ok);
    }

    [HttpGet("{publicId:guid}/edicion")]
    public async Task<IActionResult> ObtenerEdicion(Guid publicId)
    {
        var resultado =
            await _obtenerEdicion.EjecutarAsync(publicId);

        return resultado.AActionResult(Ok);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] ProductoInput input)
    {
        var resultado =
            await _crear.EjecutarAsync(input);

        return resultado.AActionResult(publicId =>
            CreatedAtAction(
                nameof(Obtener),
                new { publicId },
                new { publicId }));
    }

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(
        Guid publicId,
        [FromBody] ProductoInput input)
    {
        var resultado =
            await _actualizar.EjecutarAsync(
                publicId,
                input);

        return resultado.AActionResult();
    }

    // Ruta histórica que ya consume el Front.
    [HttpPatch("{publicId:guid}/baja")]
    public async Task<IActionResult> DarDeBaja(Guid publicId)
    {
        var resultado =
            await _cambiarEstado.EjecutarAsync(
                publicId,
                false);

        return resultado.AActionResult();
    }

    // Permite también reactivación sin cambiar la ruta /baja existente.
    [HttpPatch("{publicId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(
        Guid publicId,
        [FromQuery] bool nuevoEstado)
    {
        var resultado =
            await _cambiarEstado.EjecutarAsync(
                publicId,
                nuevoEstado);

        return resultado.AActionResult();
    }

    // Integración Ventas -> Productos.
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar(
        [FromQuery] string? termino)
    {
        var resultado =
            await _buscar.EjecutarAsync(termino);

        return resultado.AActionResult(Ok);
    }

    [HttpPost("{publicId:guid}/stock/descontar")]
    public async Task<IActionResult> DescontarStock(
        Guid publicId,
        [FromBody] StockRequest request)
    {
        var resultado =
            await _descontarStock.EjecutarAsync(
                publicId,
                request.Cantidad,
                request.VentaPublicId);

        return resultado.AActionResult(() =>
            Ok(new
            {
                mensaje = "Stock descontado correctamente."
            }));
    }

    [HttpPost("{publicId:guid}/stock/restaurar")]
    public async Task<IActionResult> RestaurarStock(
        Guid publicId,
        [FromBody] StockRequest request)
    {
        var resultado =
            await _restaurarStock.EjecutarAsync(
                publicId,
                request.Cantidad,
                request.VentaPublicId);

        return resultado.AActionResult(() =>
            Ok(new
            {
                mensaje = "Stock restaurado correctamente."
            }));
    }
}