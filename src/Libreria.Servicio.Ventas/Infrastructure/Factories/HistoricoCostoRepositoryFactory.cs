using Libreria.Servicio.Ventas.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;


namespace Libreria.Servicio.Ventas.Infrastructure
{
    public class HistoricoCostoRepositoryFactory : HistoricoRepositoryFactory
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public HistoricoCostoRepositoryFactory(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public override IHistoricoCostoRepository CrearRepositorio()
        {
            return new HistoricoCostoRepository(_connectionFactory);
        }
    }
}
