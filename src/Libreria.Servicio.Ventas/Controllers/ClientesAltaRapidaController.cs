
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Primary;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Ventas.Controllers;

[ApiController]
[Route("api/clientes")]
public sealed class ClientesAltaRapidaController : ControllerBase
{
    private readonly IClienteService _clientes;

    public ClientesAltaRapidaController(IClienteService clientes)
    {
        _clientes = clientes;
    }

    [HttpPost]
    public async Task<IActionResult> Registrar(
        [FromBody] ClienteInput input)
    {
        var resultado = await _clientes.RegistrarRapidoAsync(input);

        if (!resultado.Exitoso)
        {
            var errores = resultado.Errores.ToDictionary(
                error => error.Key,
                error => new[] { error.Value });

            return BadRequest(new ValidationProblemDetails(errores)
            {
                Title = "No se pudo registrar el cliente.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return Ok(resultado);
    }
}
