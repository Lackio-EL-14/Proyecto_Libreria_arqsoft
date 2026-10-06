using Libreria.Application.Factories;
using Libreria.Application.Models;
using Libreria.Application.Ports.Primary;
using Libreria.Application.Ports.Secondary;

namespace Libreria.Application.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public AuditoriaService(UsuarioRepositoryFactory factory)
    {
        _usuarioRepository = factory.CrearRepositorio();
    }

    public async Task<AuditoriaRegistro> ObtenerAsync(
        int? usuarioCreacionId,
        DateTime? fechaCreacion,
        int? usuarioModificacionId,
        DateTime? fechaModificacion)
    {
        var creadoPor = await ObtenerNombreAsync(usuarioCreacionId);

        var modificadoPor = usuarioModificacionId == usuarioCreacionId
            ? creadoPor
            : await ObtenerNombreAsync(usuarioModificacionId);

        return new AuditoriaRegistro(
            creadoPor,
            fechaCreacion,
            modificadoPor,
            usuarioModificacionId.HasValue ? fechaModificacion : null);
    }

    private async Task<string?> ObtenerNombreAsync(int? usuarioId)
    {
        return usuarioId.HasValue
            ? await _usuarioRepository.ObtenerNombreCompletoPorIdAsync(usuarioId.Value)
            : null;
    }
}
