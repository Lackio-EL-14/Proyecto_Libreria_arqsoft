using System.Data;
using Libreria.Web.Data;
using Libreria.Web.Pages.Historico.Models;

namespace Libreria.Web.Pages.Historico.Repositories;

public class HistoricoCostoRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public HistoricoCostoRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public string? ObtenerNombreProducto(int productoId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT Nombre
            FROM Producto
            WHERE ProductoId = @ProductoId";

        AgregarParametro(command, "@ProductoId", productoId);

        var resultado = command.ExecuteScalar();

        if (resultado == null || resultado == DBNull.Value)
        {
            return null;
        }

        return Convert.ToString(resultado);
    }

    public List<HistoricoCostoItem> ObtenerHistorico(int productoId)
    {
        var historial = new List<HistoricoCostoItem>();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            WITH HistorialOrdenado AS
            (
                SELECT
                    HistoricoCostoId,
                    CostoAdquisicion,
                    Motivo,
                    FechaVigencia,
                    LAG(CostoAdquisicion) OVER (
                        ORDER BY FechaVigencia, HistoricoCostoId
                    ) AS CostoAnterior
                FROM HistoricoCostoProducto
                WHERE ProductoId = @ProductoId
            )
            SELECT
                HistoricoCostoId,
                CostoAdquisicion,
                CostoAnterior,
                Motivo,
                FechaVigencia
            FROM HistorialOrdenado
            ORDER BY FechaVigencia DESC, HistoricoCostoId DESC";

        AgregarParametro(command, "@ProductoId", productoId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            historial.Add(new HistoricoCostoItem
            {
                HistoricoCostoId =
                    Convert.ToInt32(reader["HistoricoCostoId"]),

                CostoNuevo =
                    Convert.ToDecimal(reader["CostoAdquisicion"]),

                CostoAnterior =
                    reader["CostoAnterior"] == DBNull.Value
                        ? null
                        : Convert.ToDecimal(reader["CostoAnterior"]),

                Motivo =
                    reader["Motivo"] == DBNull.Value
                        ? null
                        : Convert.ToString(reader["Motivo"]),

                FechaVigencia =
                    Convert.ToDateTime(reader["FechaVigencia"])
            });
        }

        return historial;
    }

    private static void AgregarParametro(
        IDbCommand command,
        string nombre,
        object valor)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = nombre;
        parameter.Value = valor;
        command.Parameters.Add(parameter);
    }
}