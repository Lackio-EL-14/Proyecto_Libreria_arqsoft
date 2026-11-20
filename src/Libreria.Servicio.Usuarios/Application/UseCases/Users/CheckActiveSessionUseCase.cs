using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class CheckActiveSessionUseCase : ICheckActiveSessionUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IProveedorIdentidad _proveedorIdentidad;

    public CheckActiveSessionUseCase(
        IUserRepository userRepository, IProveedorIdentidad proveedorIdentidad)
    {
        _userRepository = userRepository;
        _proveedorIdentidad = proveedorIdentidad;
    }

    public Task<bool> ExecuteAsync(int userId)
    {
        if (userId <= 0)
        {
            return Task.FromResult(false);
        }

        return _userRepository.IsSessionActiveAsync(userId);
    }

    public async Task<bool> ExecuteAsync(Guid publicId)
    {
        if (publicId == Guid.Empty)
        {
            return false;
        }

        if (!await _userRepository.IsSessionActiveAsync(publicId))
            return false;

        // La consulta de sesión existente recibe el sub de Keycloak.
        var pending = await _proveedorIdentidad.TieneCambioDeContrasenaPendienteAsync(publicId);
        if (!pending.EsExito || pending.Valor)
            return false;

        await _userRepository.ActivatePendingAsync(publicId);
        return await _userRepository.IsSessionActiveAsync(publicId);
    }
}
