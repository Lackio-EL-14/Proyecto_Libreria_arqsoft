using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IUsuarioService
{
    Task<string> CrearPendienteAsync(UsuarioAltaInput input);
}