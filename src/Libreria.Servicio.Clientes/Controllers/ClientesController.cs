using Libreria.Servicio.Clientes.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Libreria.Servicio.Clientes.Application.Models;
using Libreria.Servicio.Clientes.Application.Ports.Primary;
using Libreria.Servicio.Clientes.Controllers.Comun;
using Libreria.Servicio.Clientes.Controllers.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Clientes.Controllers;

/// <summary>
/// Adaptador de entrada (Hexagonal): expone el puerto primario <see cref="IClienteService"/>
/// por REST (contrato 6.6) y traduce cada <c>Result</c> a HTTP con <see cref="ResultadoHttpExtensions"/>.
/// </summary>
[ApiController]
// US-126: Administrador y Vendedor gestionan clientes; solo el Administrador cambia el estado.
[Authorize(Policy = SeguridadJwtExtensions.GestionClientes)]
[Route("api/clientes")]
[Produces("application/json")]
public sealed class ClientesController : ControllerBase
{
    private readonly IClienteService _clientes;

    public ClientesController(IClienteService clientes)
    {
        _clientes = clientes;
    }

    /// <summary>
    /// Listado con búsqueda y orden. <c>estado</c>: sin enviar = activos, <c>false</c> = inactivos,
    /// vacío (<c>estado=</c>) = todos. <c>orden</c>: RazonSocial, CiNit, Correo o FechaCreacion.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ClienteDetalle>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Listar([FromQuery] FiltroClientes filtro) =>
        Ok(await _clientes.ListarAsync(filtro));

    [HttpGet("{publicId:guid}")]
    [ProducesResponseType<ClienteDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(Guid publicId) =>
        (await _clientes.ObtenerAsync(publicId)).AActionResult(cliente => Ok(cliente));

    /// <summary>Registra un cliente en la base propia del microservicio.</summary>
    [HttpPost]
    [ProducesResponseType<ClienteDetalle>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar([FromBody] ClienteInput input) =>
        (await _clientes.RegistrarAsync(input)).AActionResult(cliente => CreatedAtAction(
            nameof(Obtener),
            new { publicId = cliente.PublicId },
            cliente));

    /// <summary>Edición de CI/NIT, razón social y correo de un cliente activo.</summary>
    [HttpPut("{publicId:guid}")]
    [ProducesResponseType<ClienteDetalle>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(Guid publicId, [FromBody] ClienteInput input) =>
        (await _clientes.ActualizarAsync(publicId, input)).AActionResult(cliente => Ok(cliente));

    /// <summary>Delete lógico (<c>{ "activo": false }</c>) o reactivación (<c>{ "activo": true }</c>).</summary>
    [HttpPatch("{publicId:guid}/estado")]
    [Authorize(Policy = SeguridadJwtExtensions.SoloAdministrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CambiarEstado(Guid publicId, [FromBody] CambioEstadoCliente cambio) =>
        (await _clientes.CambiarEstadoAsync(publicId, cambio.Activo!.Value)).AActionResult();
}
