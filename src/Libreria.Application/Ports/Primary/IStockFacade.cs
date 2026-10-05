using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
namespace Libreria.Application.Ports.Primary
{
    public interface IStockFacade
    {
        void DescontarStock();
        void RestaurarStock();
    }
}
