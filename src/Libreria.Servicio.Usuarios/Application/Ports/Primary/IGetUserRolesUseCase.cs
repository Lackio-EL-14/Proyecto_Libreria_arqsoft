namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface IGetUserRolesUseCase
{
    Task<IReadOnlyList<string>?> ExecuteAsync();
}
