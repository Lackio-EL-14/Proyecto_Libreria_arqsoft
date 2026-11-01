using Libreria.Servicio.Usuarios.Application.DTOs.Auth;
using Libreria.Servicio.Usuarios.Application.UseCases.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Usuarios.Presentation.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private const string InvalidCredentialsMessage =
        "Usuario o contraseña incorrectos.";

    private readonly LoginUseCase _loginUseCase;

    public AuthController(LoginUseCase loginUseCase)
    {
        _loginUseCase = loginUseCase;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request)
    {
        try
        {
            var user = await _loginUseCase.ExecuteAsync(request);

            if (user is null)
            {
                return Problem(
                    title: "No fue posible iniciar sesión.",
                    detail: InvalidCredentialsMessage,
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return Ok(user);
        }
        catch (ArgumentException exception)
        {
            return Problem(
                title: "Solicitud inválida.",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}