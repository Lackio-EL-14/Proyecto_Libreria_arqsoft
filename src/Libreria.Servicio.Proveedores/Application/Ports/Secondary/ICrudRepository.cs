namespace Libreria.Servicio.Proveedores.Application.Ports.Secondary;

public interface ICrudRepository<TEntity> where TEntity : class
{
    Task CrearAsync(TEntity entidad);
    Task<bool> ActualizarAsync(TEntity entidad);
    Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado);
    Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null);
}
