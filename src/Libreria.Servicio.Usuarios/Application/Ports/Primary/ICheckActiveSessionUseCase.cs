namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface ICheckActiveSessionUseCase
{
    Task<bool> ExecuteAsync(int userId);

    Task<bool> ExecuteAsync(Guid publicId);
}
