namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface IGetUserNamesUseCase
{
    Task<IReadOnlyDictionary<int, string>> ExecuteAsync(IEnumerable<int> userIds);
    Task<IReadOnlyDictionary<Guid, string>> ExecuteAsync(IEnumerable<Guid> publicIds);
}
