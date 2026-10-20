using Libreria.Application.Factories;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Validators;
using Libreria.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Libreria.Application.Services;

public class AutenticacionService : IAutenticacionService
{
    private readonly IUsuarioRepository _repository;
    private readonly IContrasenaHasher _hasher;
    private readonly LoginValidator _validator;
    private readonly ILogger<AutenticacionService> _logger;

    public AutenticacionService(
        UsuarioRepositoryFactory factory,
        IContrasenaHasher hasher,
        LoginValidator validator,
        ILogger<AutenticacionService>? logger = null)
    {
        _repository = factory.CrearRepositorio();
        _hasher = hasher;
        _validator = validator;
        _logger = logger ?? NullLogger<AutenticacionService>.Instance;
    }

    public async Task<ResultadoAutenticacion> AutenticarAsync(LoginInput input)
    {
        _validator.Normalizar(input);

        var errores = _validator.Validar(input);
        if (errores.Count > 0)
        {
            return ResultadoAutenticacion.Invalido(errores);
        }

        var usuario = await _repository.ObtenerActivoPorNombreUsuarioAsync(input.NombreUsuario);

        // Mismo mensaje si el usuario no existe o si la contraseña no coincide:
        // así no se revela qué dato falló (evita enumeración de usuarios).
        if (usuario is null || usuario.Estado != EstadosUsuario.Activo || !usuario.Rol.Estado ||
            !_hasher.Verificar(usuario.PasswordHash, input.Contrasena))
        {
            return ResultadoAutenticacion.CredencialesInvalidas();
        }

        await MigrarHashSiEsNecesarioAsync(usuario, input.Contrasena);

        return ResultadoAutenticacion.Correcto(new UsuarioAutenticado(
            usuario.UsuarioId,
            usuario.PublicId,
            usuario.NombreUsuario,
            usuario.NombreCompleto,
            usuario.Rol.Nombre));
    }

    private async Task MigrarHashSiEsNecesarioAsync(
        Usuario usuario,
        string contrasena)
    {
        if (!_hasher.RequiereRehash(usuario.PasswordHash))
        {
            return;
        }

        try
        {
            var actualizado = await _repository.ActualizarContrasenaAsync(
                usuario.UsuarioId,
                usuario.PasswordHash,
                _hasher.Hashear(contrasena));

            if (actualizado)
            {
                _logger.LogInformation(
                    "Hash de contraseña migrado a Argon2id para UsuarioId={UsuarioId}",
                    usuario.UsuarioId);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "No fue posible migrar el hash de contraseña para UsuarioId={UsuarioId}; el inicio de sesión continuará",
                usuario.UsuarioId);
        }
    }
}
