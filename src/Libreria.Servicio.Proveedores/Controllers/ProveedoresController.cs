using Libreria.Servicio.Proveedores.Application.Models;
using Libreria.Servicio.Proveedores.Application.Ports.Primary;
using Libreria.Servicio.Proveedores.Controllers.Comun;
using Libreria.Servicio.Proveedores.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Proveedores.Controllers;

[ApiController]
[Route("api/proveedores")]
[Produces("application/json")]
public sealed class ProveedoresController : ControllerBase
{
    private readonly IProveedorService _proveedores;
    public ProveedoresController(IProveedorService proveedores) => _proveedores = proveedores;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] FiltroProveedores filtro) => Ok(await _proveedores.ListarAsync(filtro));

    [HttpGet("{publicId:guid}")]
    public async Task<IActionResult> Obtener(Guid publicId) =>
        (await _proveedores.ObtenerAsync(publicId)).AActionResult(Ok);

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] ProveedorInput input) =>
        (await _proveedores.RegistrarAsync(input)).AActionResult(p => CreatedAtAction(nameof(Obtener), new { publicId = p.PublicId }, p));

    [HttpPut("{publicId:guid}")]
    public async Task<IActionResult> Actualizar(Guid publicId, [FromBody] ProveedorInput input) =>
        (await _proveedores.ActualizarAsync(publicId, input)).AActionResult(Ok);

    [HttpPatch("{publicId:guid}/estado")]
    public async Task<IActionResult> CambiarEstado(Guid publicId, [FromBody] CambioEstadoProveedor cambio) =>
        (await _proveedores.CambiarEstadoAsync(publicId, cambio.Activo!.Value)).AActionResult();
}
