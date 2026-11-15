using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;

namespace Libreria.Servicio.Ventas.Application.Factories
{
    public abstract class HistoricoRepositoryFactory
    {
        // Único método fábrica, específico para el Histórico
        public abstract IHistoricoCostoRepository CrearRepositorio();
    }
}
