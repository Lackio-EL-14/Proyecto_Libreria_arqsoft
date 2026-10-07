using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
namespace Libreria.Application.Ports.Primary
{
    public interface IVentaFacade
    {
        Task<VentaRegistradaResult> RegistrarVentaAsync(
            RegistrarVentaInput input);
        void AnularVenta();
    }
}
