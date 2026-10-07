using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using System.Data;
namespace Libreria.Application.Ports.Primary
{
    public interface IStockFacade
    {
        Task DescontarStockAsync(
            int productoId,
            int cantidad,
            IDbConnection connection,
            IDbTransaction transaction);
        void RestaurarStock();
    }
}
