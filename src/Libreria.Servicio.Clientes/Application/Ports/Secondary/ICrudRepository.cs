namespace Libreria.Servicio.Clientes.Application.Ports.Secondary;

/// <summary>Puerto CRUD genérico (copia del de Ventas; no hay bibliotecas compartidas).</summary>
public interface ICrudRepository<TEntity> where TEntity : class
{
    Task<IReadOnlyList<TEntity>> ObtenerActivasAsync(string? busqueda = null);
    Task<TEntity?> ObtenerPorPublicIdAsync(Guid publicId, bool estadoEsperado);
    Task CrearAsync(TEntity entidad);
    Task<bool> ActualizarAsync(TEntity entidad);
    Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado);
    Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null);
    Task<bool> TieneRelacionesAsync(Guid publicId);
}
