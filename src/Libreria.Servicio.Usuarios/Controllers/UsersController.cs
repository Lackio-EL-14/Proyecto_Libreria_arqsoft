using Libreria.Servicio.Usuarios.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Presentation.Comun;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Microsoft.AspNetCore.Mvc;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;

namespace Libreria.Servicio.Usuarios.Controllers;

[ApiController]
// US-126: todo exige token; listar, crear, ver roles y dar de baja son solo del Administrador.
// Cambiar la contraseña, la sesión activa y los nombres (auditoría) sirven a cualquier usuario con sesión.
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
    [Authorize(Policy = SeguridadJwtExtensions.SoloAdministrador)]
    [ProducesResponseType(
        typeof(IReadOnlyList<UserListItemDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsersAsync(
        [FromQuery] string? estado = null,
        [FromQuery] string? orden = null,
        [FromQuery] bool descendente = false)
    {
        var resultado = await _listUsersUseCase.ExecuteAsync(estado, orden, descendente);

        if (resultado is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return resultado.AActionResult(users => Ok(users));
    }

    [HttpGet("{id:int}/sesion-activa")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> IsSessionActiveAsync(int id)
    {
        var isActive =
            await _checkActiveSessionUseCase.ExecuteAsync(id);

        return Ok(isActive);
    }

    // PublicId es el contrato entre servicios; se conserva la variante numérica
    // únicamente por compatibilidad con consumidores todavía no migrados.
    [HttpGet("{publicId:guid}/sesion-activa")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<ActionResult<bool>> IsSessionActiveByPublicIdAsync(Guid publicId)
    {
        var isActive = await _checkActiveSessionUseCase.ExecuteAsync(publicId);
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
    [Authorize(Policy = SeguridadJwtExtensions.SoloAdministrador)]
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
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] CreateUserRequest request)
    {
        var result = await _createUserUseCase.ExecuteAsync(request);

        if (result is null)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return result.AActionResult(
            created => StatusCode(StatusCodes.Status201Created, created));
    }

    [HttpGet("roles")]
    [Authorize(Policy = SeguridadJwtExtensions.SoloAdministrador)]
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
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordRequest request)
    {
        var resultado =
            await _changePasswordUseCase.ExecuteAsync(request);

        if (resultado is null)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden);
        }

        // 204, o 400/404 con los mensajes del Result (Detail lleva el texto que muestra el Front).
        return resultado.AActionResult();
    }

    [HttpDelete("{publicId:guid}")]
    [Authorize(Policy = SeguridadJwtExtensions.SoloAdministrador)]
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

            DeactivateUserResult.IdentityProviderUnavailable =>
                Problem(
                    title: "No fue posible dar de baja al usuario.",
                    detail: "El proveedor de identidad no respondió; no se realizó ningún cambio.",
                    statusCode: StatusCodes.Status503ServiceUnavailable),

            _ => throw new InvalidOperationException(
                "Resultado de baja de usuario no reconocido.")
        };
    }
}
