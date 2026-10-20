using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IUsuarioService
{
    Task<string> CrearPendienteAsync(UsuarioAltaInput input);
    Task<ResultadoOperacion> CambiarContrasenaAsync(CambioContrasenaInput input);
    Task ActivarAsync(UsuarioActivacionInput input);
}
