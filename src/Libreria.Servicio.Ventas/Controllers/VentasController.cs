using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using System.Threading.Tasks;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VentasController : ControllerBase
{
    private readonly IVentaFacade _ventaFacade;

    public VentasController(IVentaFacade ventaFacade)
    {
        _ventaFacade = ventaFacade;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { mensaje = "Servicio de Ventas SBA operativo" });
    }
    
    // Aquí iremos mapeando los métodos de IVentaFacade (Registrar, Obtener, etc.)
}
