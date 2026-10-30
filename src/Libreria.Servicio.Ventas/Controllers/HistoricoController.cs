using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using System;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HistoricoController : ControllerBase
{
    private readonly IHistoricoCostoRepository _historicoRepository;

    public HistoricoController(IHistoricoCostoRepository historicoRepository)
    {
        _historicoRepository = historicoRepository;
    }

    [HttpGet("{productoId:guid}")]
    public IActionResult ObtenerPorProducto(Guid productoId)
    {
        var historico = _historicoRepository.ObtenerProducto(productoId);
        if (historico == null) return NotFound();
        return Ok(historico);
    }
}
