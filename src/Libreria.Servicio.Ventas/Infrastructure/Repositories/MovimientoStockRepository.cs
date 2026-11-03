
using System.Data;
using System.Data.Common;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;

namespace Libreria.Servicio.Ventas.Infrastructure;

public class MovimientoStockRepository : IMovimientoStockRepository
{
    public async Task RegistrarAsync(
        int ventaId,
        int productoId,
        int cantidad,
        string tipoMovimiento,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        if (ventaId <= 0 || productoId <= 0 || cantidad <= 0)
            throw new ArgumentException("Datos del movimiento inválidos.");

        if (tipoMovimiento != "Venta" && tipoMovimiento != "Anulacion")
            throw new ArgumentException("Tipo de movimiento inválido.");

        if (connection is not DbConnection dbConnection ||
            transaction is not DbTransaction dbTransaction)
            throw new ArgumentException("Conexión o transacción inválida.");

        await using var command = dbConnection.CreateCommand();
        command.Transaction = dbTransaction;

        command.CommandText = @"
            INSERT INTO dbo.HistoricoMovimientoStock
                (VentaId, ProductoId, Cantidad, TipoMovimiento)
            VALUES
                (@VentaId, @ProductoId, @Cantidad, @TipoMovimiento)";

        AgregarParametro(command, "@VentaId", ventaId);
        AgregarParametro(command, "@ProductoId", productoId);
        AgregarParametro(command, "@Cantidad", cantidad);
        AgregarParametro(command, "@TipoMovimiento", tipoMovimiento);

        await command.ExecuteNonQueryAsync();
    }

    private static void AgregarParametro(
        DbCommand command, string nombre, object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}
