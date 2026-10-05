using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;

namespace Libreria.Application.Factories
{
    public abstract class HistoricoRepositoryFactory
    {
        // Único método fábrica, específico para el Histórico
        public abstract IHistoricoCostoRepository CrearRepositorio();
    }
}
