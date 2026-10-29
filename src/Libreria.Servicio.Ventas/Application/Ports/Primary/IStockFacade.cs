using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
using System.Data;
namespace Libreria.Servicio.Ventas.Application.Ports.Primary
{
    public interface IStockFacade
    {
        Task DescontarStockAsync(
            int productoId,
            int cantidad,
            IDbConnection connection,
            IDbTransaction transaction);
        Task RestaurarStockAsync(
            int productoId,
            int cantidad,
            IDbConnection connection,
            IDbTransaction transaction);
    }
}
