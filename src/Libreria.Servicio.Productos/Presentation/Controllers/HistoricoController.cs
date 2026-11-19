using Libreria.Servicio.Productos.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Libreria.Servicio.Productos.Application.UseCases.Historico;
using Libreria.Servicio.Productos.Presentation.Comun;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Productos.Presentation.Controllers;

[ApiController]
// US-126: solo el Administrador gestiona el inventario.
[Authorize(Policy = SeguridadJwtExtensions.SoloAdministrador)]
[Route("api/historico")]
public sealed class HistoricoController : ControllerBase
{
    private readonly ObtenerHistoricoProductoUseCase _obtener;

    public HistoricoController(
        ObtenerHistoricoProductoUseCase obtener)
    {
        _obtener = obtener;
    }

    [HttpGet("{productoPublicId:guid}")]
    public async Task<IActionResult> Obtener(
        Guid productoPublicId)
    {
        var resultado =
            await _obtener.EjecutarAsync(
                productoPublicId);

        return resultado.AActionResult(Ok);
    }
}