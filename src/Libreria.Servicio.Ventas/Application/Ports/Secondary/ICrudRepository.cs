using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Libreria.Servicio.Ventas.Application.Ports.Secondary
{
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
}
