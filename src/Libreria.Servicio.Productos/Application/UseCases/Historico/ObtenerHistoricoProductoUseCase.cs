using Libreria.Servicio.Productos.Application.Comun;
using Libreria.Servicio.Productos.Application.Factories;
using Libreria.Servicio.Productos.Application.Models.Historico;

namespace Libreria.Servicio.Productos.Application.UseCases.Historico;

public sealed class ObtenerHistoricoProductoUseCase
{
    private readonly HistoricoRepositoryFactory _factory;

    public ObtenerHistoricoProductoUseCase(
        HistoricoRepositoryFactory factory)
    {
        _factory = factory;
    }

    public async Task<Result<HistoricoProducto>> EjecutarAsync(
        Guid productoPublicId)
    {
        if (productoPublicId == Guid.Empty)
        {
            return Result<HistoricoProducto>.Fallo(
                Result.Validacion(
                    "ProductoPublicId",
                    "Seleccione un producto válido."));
        }

        var repository = _factory.CrearRepositorio();

        var producto =
            await repository.ObtenerProductoAsync(
                productoPublicId);

        if (producto is null)
        {
            return Result<HistoricoProducto>.Fallo(
                Result.NoEncontrado(
                    "El producto no existe."));
        }

        var registros =
            await repository.ObtenerHistoricoAsync(
                productoPublicId);

        // Calculamos costo anterior y variación cronológicamente.
        var ordenados = registros
            .OrderBy(item => item.FechaVigencia)
            .ToList();

        var historial =
            new List<HistoricoCostoItem>();

        decimal? costoAnterior = null;

        foreach (var item in ordenados)
        {
            var variacion = costoAnterior.HasValue
                ? item.CostoAdquisicion - costoAnterior.Value
                : (decimal?)null;

            historial.Add(
                new HistoricoCostoItem(
                    item.FechaVigencia,
                    item.CostoAdquisicion,
                    costoAnterior,
                    variacion,
                    item.FechaVencimiento,
                    item.Motivo,
                    item.UsuarioCreacionId == Guid.Empty
                        ? null
                        : item.UsuarioCreacionId.ToString()));

            costoAnterior = item.CostoAdquisicion;
        }

        // En pantalla mostramos lo más reciente primero.
        historial.Reverse();

        return Result<HistoricoProducto>.Exito(
            new HistoricoProducto(
                producto.PublicId,
                producto.Nombre,
                producto.Estado,
                historial));
    }
}