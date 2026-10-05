using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IAutenticacionService
{
    Task<ResultadoAutenticacion> AutenticarAsync(LoginInput input);
}
