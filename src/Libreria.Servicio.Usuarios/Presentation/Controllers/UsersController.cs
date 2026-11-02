using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Presentation.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsersController : ControllerBase
{
    private readonly CheckActiveSessionUseCase _checkActiveSessionUseCase;
    private readonly GetUserNamesUseCase _getUserNamesUseCase;
    private readonly ListUsersUseCase _listUsersUseCase;
    private readonly CreateUserUseCase _createUserUseCase;
    private readonly ActivateUserUseCase _activateUserUseCase;
    private readonly GetUserRolesUseCase _getUserRolesUseCase;
    private readonly ChangePasswordUseCase _changePasswordUseCase;

    public UsersController(
        CheckActiveSessionUseCase checkActiveSessionUseCase,
        GetUserNamesUseCase getUserNamesUseCase,
        ListUsersUseCase listUsersUseCase,CreateUserUseCase createUserUseCase,
        ActivateUserUseCase activateUserUseCase,
        GetUserRolesUseCase getUserRolesUseCase,ChangePasswordUseCase changePasswordUseCase)
    {
        _checkActiveSessionUseCase = checkActiveSessionUseCase;
        _getUserNamesUseCase = getUserNamesUseCase;
        _listUsersUseCase = listUsersUseCase;
        _createUserUseCase = createUserUseCase;
        _activateUserUseCase = activateUserUseCase;
        _getUserRolesUseCase = getUserRolesUseCase;
        _changePasswordUseCase = changePasswordUseCase;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<UserListItemDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> GetUsersAsync(
        [FromQuery] string? estado = null)
    {
        try
        {
            var users =
                await _listUsersUseCase.ExecuteAsync(estado);

           if (users is null)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            return Ok(users);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                title: "Solicitud inválida.",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
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

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateUserResultDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateUserResultDto>>
        CreateUserAsync(
            [FromBody] CreateUserRequest request)
    {
        try
        {
            var result =
                await _createUserUseCase.ExecuteAsync(request);

            if (result is null)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden);
            }

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                title: "Solicitud inválida.",
                detail: exception.Message,
                statusCode:
                    StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                title: "No fue posible registrar el usuario.",
                detail: exception.Message,
                statusCode:
                    StatusCodes.Status409Conflict);
        }
    }

    [HttpGet("roles")]
    [ProducesResponseType(
        typeof(IReadOnlyList<string>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<string>>>
        GetRolesAsync()
    {
        var roles =
            await _getUserRolesUseCase.ExecuteAsync();

        if (roles is null)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden);
        }

        return Ok(roles);
    }


    [HttpPost("activar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ActivateUserAsync(
        [FromBody] ActivateUserRequest request)
    {
        try
        {
            await _activateUserUseCase.ExecuteAsync(request);

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return Problem(
                title: "No fue posible activar la cuenta.",
                detail: exception.Message,
                statusCode:
                    StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                title: "No fue posible activar la cuenta.",
                detail: exception.Message,
                statusCode:
                    StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("contrasena")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request)
    {
        try
        {
            var changed =
                await _changePasswordUseCase.ExecuteAsync(request);

            if (!changed)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden);
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return Problem(
                title: "No fue posible cambiar la contraseña.",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException exception)
        {
            return Problem(
                title: "No fue posible cambiar la contraseña.",
                detail: exception.Message,
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}