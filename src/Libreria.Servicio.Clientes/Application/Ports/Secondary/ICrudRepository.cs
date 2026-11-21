namespace Libreria.Servicio.Clientes.Application.Ports.Secondary;

/// <summary>Puerto CRUD genérico propio del microservicio.</summary>
public interface ICrudRepository<TEntity> where TEntity : class
{
    Task CrearAsync(TEntity entidad);
    Task<bool> ActualizarAsync(TEntity entidad);
    Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado);
    Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null);
}
