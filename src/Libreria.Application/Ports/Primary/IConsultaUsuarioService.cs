using Libreria.Application.Models;

namespace Libreria.Application.Ports.Primary;

public interface IConsultaUsuarioService
{
    Task<IReadOnlyList<UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null);
}
