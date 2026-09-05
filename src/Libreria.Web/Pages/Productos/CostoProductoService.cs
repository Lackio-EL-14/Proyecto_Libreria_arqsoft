using Libreria.Web.Data;
using System.Data;
using Libreria.Web.Data;

namespace Libreria.Web.Pages.Productos;

public class CostoProductoService
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CostoProductoService(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }


    public bool ActualizarCostoSiCambio(
    int productoId,
    decimal nuevoCosto,
    string motivo)
    {
        if (nuevoCosto < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nuevoCosto),
                "El costo de adquisición no puede ser negativo.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            throw new ArgumentException(
                "El motivo del cambio de costo es obligatorio.",
                nameof(motivo));
        }

        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            decimal costoVigente;

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                command.CommandText = @"
                SELECT CostoAdquisicionActual
                FROM Producto
                WHERE ProductoId = @ProductoId";

                AgregarParametro(command, "@ProductoId", productoId);

                var resultado = command.ExecuteScalar();

                if (resultado is null || resultado == DBNull.Value)
                {
                    throw new InvalidOperationException(
                        "No se encontró el producto solicitado.");
                }

                costoVigente = Convert.ToDecimal(resultado);
            }

            if (costoVigente == nuevoCosto)
            {
                transaction.Rollback();
                return false;
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                command.CommandText = @"
                UPDATE Producto
                SET CostoAdquisicionActual = @NuevoCosto,
                    FechaModificacion = SYSDATETIME()
                WHERE ProductoId = @ProductoId";

                AgregarParametro(command, "@NuevoCosto", nuevoCosto);
                AgregarParametro(command, "@ProductoId", productoId);

                command.ExecuteNonQuery();
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;

                command.CommandText = @"
                INSERT INTO HistoricoCostoProducto
                (
                    ProductoId,
                    CostoAdquisicion,
                    Motivo
                )
                VALUES
                (
                    @ProductoId,
                    @NuevoCosto,
                    @Motivo
                )";

                AgregarParametro(command, "@ProductoId", productoId);
                AgregarParametro(command, "@NuevoCosto", nuevoCosto);
                AgregarParametro(command, "@Motivo", motivo);

                command.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void AgregarParametro(
    IDbCommand command,
    string nombre,
    object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }

    public void RegistrarCostoInicial(
    int productoId,
    decimal costoInicial,
    IDbConnection connection,
    IDbTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = @"
        INSERT INTO HistoricoCostoProducto
        (
            ProductoId,
            CostoAdquisicion,
            Motivo
        )
        VALUES
        (
            @ProductoId,
            @CostoAdquisicion,
            @Motivo
        )";

        AgregarParametro(command, "@ProductoId", productoId);
        AgregarParametro(command, "@CostoAdquisicion", costoInicial);
        AgregarParametro(command, "@Motivo", "Registro inicial");

        command.ExecuteNonQuery();
    }
}