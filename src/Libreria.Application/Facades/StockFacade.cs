using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Ports.Primary;

namespace Libreria.Application.Facades
{
    public class StockFacade : IStockFacade
    {
        public void DescontarStock() => throw new System.NotImplementedException();
        public void RestaurarStock() => throw new System.NotImplementedException();
    }
}
