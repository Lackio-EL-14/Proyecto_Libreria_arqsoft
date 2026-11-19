using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.DTOs.Auth;
using Libreria.Servicio.Usuarios.Application.UseCases.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Servicio.Usuarios.Controllers;

// TRANSITORIO (US-124 por etapas): se retira en US-125, cuando el Front inicie sesión con OIDC
// contra Keycloak. Ver docs/contratos/keycloak.md, «Retiro del login transitorio».
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ILoginUseCase _loginUseCase;

    public AuthController(ILoginUseCase loginUseCase)
    {
        _loginUseCase = loginUseCase;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticatedUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request)
    {
        var result = await _loginUseCase.ExecuteAsync(request);

        if (result.EsExito)
        {
            return Ok(result.Valor);
        }

        // Conserva el contrato de error actual del Front (ProblemDetails y HTTP 400).
        var invalidCredentials = result.Errores.TryGetValue("Credenciales", out var message);
        return Problem(
            title: invalidCredentials ? "No fue posible iniciar sesión." : "Solicitud inválida.",
            detail: invalidCredentials ? message : string.Join(" ", result.Errores.Values),
            statusCode: StatusCodes.Status400BadRequest);
    }
}