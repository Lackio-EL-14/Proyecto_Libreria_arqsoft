using Libreria.Web.Data;
using Libreria.Web.Pages.Productos.Models;
using Libreria.Web.Pages.Productos.Repositories;

namespace Libreria.Web.Pages.Productos.Services;

public class ProductoService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IProductoRepository _repository;
    private readonly CostoProductoService _costoProductoService;

    public ProductoService(
        IDbConnectionFactory connectionFactory,
        IProductoRepository repository,
        CostoProductoService costoProductoService)
    {
        _connectionFactory = connectionFactory;
        _repository = repository;
        _costoProductoService = costoProductoService;
    }

    public void RegistrarProductoConHistorico(ProductoInput input)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            int productoId = _repository.CrearProducto(
                input,
                connection,
                transaction);

            _costoProductoService.RegistrarCostoInicial(
                productoId,
                input.CostoAdquisicion,
                connection,
                transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}