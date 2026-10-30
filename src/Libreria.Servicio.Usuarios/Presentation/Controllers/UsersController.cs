using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Usuarios.Presentation.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsersController : ControllerBase
{
    private readonly CheckActiveSessionUseCase _checkActiveSessionUseCase;
    private readonly GetUserNamesUseCase _getUserNamesUseCase;
    public UsersController(
    CheckActiveSessionUseCase checkActiveSessionUseCase,
    GetUserNamesUseCase getUserNamesUseCase)
    {
        _checkActiveSessionUseCase =
            checkActiveSessionUseCase;

        _getUserNamesUseCase =
            getUserNamesUseCase;
    }

    [HttpGet("{id:int}/sesion-activa")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> IsSessionActiveAsync(int id)
    {
        var isActive =
            await _checkActiveSessionUseCase.ExecuteAsync(id);

        return Ok(isActive);
    }

    [HttpGet("nombres")]
    [ProducesResponseType(
    typeof(IReadOnlyDictionary<int, string>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    typeof(ProblemDetails),
    StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyDictionary<int, string>>>
    GetNamesAsync([FromQuery(Name = "ids")] string[] ids)
    {
        var parsedIds = new List<int>();

        foreach (var value in ids)
        {
            var parts = value.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var userId) ||
                    userId <= 0)
                {
                    return Problem(
                        title: "Solicitud inválida.",
                        detail: "Los identificadores de usuario deben ser números positivos.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                parsedIds.Add(userId);
            }
        }

        var names =
            await _getUserNamesUseCase.ExecuteAsync(parsedIds);

        return Ok(names);
    }
}