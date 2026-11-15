using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using System.Threading.Tasks;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;
    private readonly IClienteRepository _clienteRepository;

    public ClientesController(IClienteService clienteService, IClienteRepository clienteRepository)
    {
        _clienteService = clienteService;
        _clienteRepository = clienteRepository;
    }

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string ciNit)
    {
        if (string.IsNullOrWhiteSpace(ciNit)) return BadRequest("El parámetro ciNit es requerido.");
        
        var clientes = await _clienteRepository.BuscarPorCiNitAsync(ciNit);
        return Ok(clientes);
    }
}
