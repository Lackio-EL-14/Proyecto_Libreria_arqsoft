using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;
using Libreria.Domain;

namespace Libreria.Application.Services;

public class ConsultaUsuarioService : IConsultaUsuarioService
{
    private readonly IUsuarioRepository _repository;

    public ConsultaUsuarioService(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<UsuarioListItemModel>> ObtenerListadoAsync(string? estado = null)
    {
        var filtro = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim();

        if (filtro is not null && !EstadosUsuario.EsValido(filtro))
        {
            throw new ArgumentException("Seleccione un estado de usuario válido.", nameof(estado));
        }

        return _repository.ObtenerListadoAsync(filtro);
    }
}
