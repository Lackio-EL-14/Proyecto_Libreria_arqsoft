using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;


namespace Libreria.Infrastructure
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
