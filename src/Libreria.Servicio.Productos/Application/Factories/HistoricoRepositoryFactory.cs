using Libreria.Servicio.Productos.Application.Abstractions;

namespace Libreria.Servicio.Productos.Application.Factories;

public abstract class HistoricoRepositoryFactory
{
    public abstract IHistoricoCostoRepository CrearRepositorio();
}