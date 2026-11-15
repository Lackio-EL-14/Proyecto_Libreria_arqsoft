using Libreria.Servicio.Ventas.Application.Factories;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/historico")]
public class HistoricoController : ControllerBase
{
    private readonly IHistoricoCostoRepository _repositorio;

    // Factory Method específico del Histórico (HistoricoRepositoryFactory).
    public HistoricoController(HistoricoRepositoryFactory fabrica)
    {
        _repositorio = fabrica.CrearRepositorio();
    }

    [HttpGet("{publicId:guid}")]
    public IActionResult ObtenerPorProducto(Guid publicId)
    {
        var producto = _repositorio.ObtenerProducto(publicId);
        if (producto is null)
        {
            return NotFound();
        }

        var historial = _repositorio.ObtenerHistorico(producto.ProductoId)
            .Select(item => new HistoricoCostoResponse(
                item.FechaVigencia,
                item.CostoNuevo,
                item.CostoAnterior,
                item.VariacionAbsoluta,
                item.FechaVencimiento,
                item.Motivo,
                item.RegistradoPor))
            .ToList();

        return Ok(new HistoricoProductoResponse(publicId, producto.Nombre, producto.Estado, historial));
    }
}
