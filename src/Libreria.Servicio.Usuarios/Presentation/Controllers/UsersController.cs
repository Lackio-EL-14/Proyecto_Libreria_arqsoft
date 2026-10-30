using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Usuarios.Presentation.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsersController : ControllerBase
{
    private readonly CheckActiveSessionUseCase _checkActiveSessionUseCase;

    public UsersController(
        CheckActiveSessionUseCase checkActiveSessionUseCase)
    {
        _checkActiveSessionUseCase = checkActiveSessionUseCase;
    }

    [HttpGet("{id:int}/sesion-activa")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> IsSessionActiveAsync(int id)
    {
        var isActive =
            await _checkActiveSessionUseCase.ExecuteAsync(id);

        return Ok(isActive);
    }
}