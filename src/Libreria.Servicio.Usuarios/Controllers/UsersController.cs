using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsersController : ControllerBase
{
    private readonly ICheckActiveSessionUseCase _checkActiveSessionUseCase;
    private readonly IGetUserNamesUseCase _getUserNamesUseCase;
    private readonly IListUsersUseCase _listUsersUseCase;
    private readonly ICreateUserUseCase _createUserUseCase;
    private readonly IGetUserRolesUseCase _getUserRolesUseCase;
    private readonly IChangePasswordUseCase _changePasswordUseCase;
    private readonly IDeactivateUserUseCase _deactivateUserUseCase;

    public UsersController(
        ICheckActiveSessionUseCase checkActiveSessionUseCase,
        IGetUserNamesUseCase getUserNamesUseCase,
        IListUsersUseCase listUsersUseCase,
        ICreateUserUseCase createUserUseCase,
        IGetUserRolesUseCase getUserRolesUseCase,
        IChangePasswordUseCase changePasswordUseCase,
        IDeactivateUserUseCase deactivateUserUseCase)
    {
        _checkActiveSessionUseCase = checkActiveSessionUseCase;
        _getUserNamesUseCase = getUserNamesUseCase;
        _listUsersUseCase = listUsersUseCase;
        _createUserUseCase = createUserUseCase;
        _getUserRolesUseCase = getUserRolesUseCase;
        _changePasswordUseCase = changePasswordUseCase;
        _deactivateUserUseCase = deactivateUserUseCase;
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

    // Contrato de auditoria: GET api/usuarios/nombres?ids=<publicId>,<publicId>.
    // Conserva compatibilidad con los consumidores que aun envian UsuarioId (int).
    [HttpGet("nombres")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNamesAsync(
        [FromQuery(Name = "ids")] string[] ids)
    {
        var parts = ids
            .SelectMany(value => value.Split(',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries))
            .ToArray();

        if (parts.Length == 0)
        {
            return Ok(new Dictionary<Guid, string>());
        }

        if (parts.All(part => Guid.TryParse(part, out var id) && id != Guid.Empty))
        {
            var publicIds = parts.Select(part => Guid.Parse(part)).ToArray();
            return Ok(await _getUserNamesUseCase.ExecuteAsync(publicIds));
        }

        if (parts.All(part => int.TryParse(part, out var id) && id > 0))
        {
            var userIds = parts.Select(part => int.Parse(part)).ToArray();
            return Ok(await _getUserNamesUseCase.ExecuteAsync(userIds));
        }

        return Problem(
            title: "Solicitud inválida.",
            detail: "Los identificadores deben ser GUID válidos o números positivos, sin mezclar formatos.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    [HttpGet("nombres-publicos")]
    [ProducesResponseType(
        typeof(IReadOnlyDictionary<Guid, string>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyDictionary<Guid, string>>> GetPublicNamesAsync(
        [FromQuery(Name = "ids")] string[] ids)
    {
        var parsedIds = new List<Guid>();

        foreach (var value in ids)
        {
            foreach (var part in value.Split(',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!Guid.TryParse(part, out var publicId) || publicId == Guid.Empty)
                {
                    return Problem(
                        title: "Solicitud inválida.",
                        detail: "Los identificadores públicos deben ser GUID válidos.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                parsedIds.Add(publicId);
            }
        }

        return Ok(await _getUserNamesUseCase.ExecuteAsync(parsedIds));
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

    [HttpDelete("{publicId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateUserAsync(
        Guid publicId)
    {
        var result =
            await _deactivateUserUseCase.ExecuteAsync(publicId);

        return result switch
        {
            DeactivateUserResult.Success =>
                NoContent(),

            DeactivateUserResult.NotFound =>
                Problem(
                    title: "Usuario no encontrado.",
                    detail: "No se encontró el usuario solicitado.",
                    statusCode: StatusCodes.Status404NotFound),

            DeactivateUserResult.ActorNotAuthorized =>
                StatusCode(StatusCodes.Status403Forbidden),

            DeactivateUserResult.AlreadyInactive =>
                Problem(
                    title: "No fue posible dar de baja al usuario.",
                    detail: "El usuario ya se encuentra inactivo.",
                    statusCode: StatusCodes.Status409Conflict),

            DeactivateUserResult.LastActiveAdministrator =>
                Problem(
                    title: "No fue posible dar de baja al usuario.",
                    detail: "No se puede dar de baja al último administrador activo.",
                    statusCode: StatusCodes.Status409Conflict),

            DeactivateUserResult.SelfDeactivation =>
                Problem(
                    title: "No fue posible dar de baja al usuario.",
                    detail: "No puede darse de baja a sí mismo.",
                    statusCode: StatusCodes.Status409Conflict),

            _ => throw new InvalidOperationException(
                "Resultado de baja de usuario no reconocido.")
        };
    }
}
