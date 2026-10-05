using Libreria.Application.Factories;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Validators;

namespace Libreria.Application.Services;

public class AutenticacionService : IAutenticacionService
{
    private readonly IUsuarioRepository _repository;
    private readonly IContrasenaHasher _hasher;
    private readonly LoginValidator _validator;

    public AutenticacionService(
        UsuarioRepositoryFactory factory,
        IContrasenaHasher hasher,
        LoginValidator validator)
    {
        _repository = factory.CrearRepositorio();
        _hasher = hasher;
        _validator = validator;
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
        if (usuario is null || !_hasher.Verificar(usuario.PasswordHash, input.Contrasena))
        {
            return ResultadoAutenticacion.CredencialesInvalidas();
        }

        return ResultadoAutenticacion.Correcto(new UsuarioAutenticado(
            usuario.UsuarioId,
            usuario.PublicId,
            usuario.NombreUsuario,
            usuario.NombreCompleto,
            usuario.Rol.Nombre));
    }
}
