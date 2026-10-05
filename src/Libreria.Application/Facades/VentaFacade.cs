using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Libreria.Application.Ports.Primary;

namespace Libreria.Application.Facades
{
    public class VentaFacade : IVentaFacade
    {
        private readonly IStockFacade _stockFacade;

        // Inyección de dependencia: La fachada principal orquesta a la secundaria
        public VentaFacade(IStockFacade stockFacade)
        {
            _stockFacade = stockFacade;
        }

        public void RegistrarVenta() => throw new System.NotImplementedException();
        public void AnularVenta() => throw new System.NotImplementedException();
    }
}
