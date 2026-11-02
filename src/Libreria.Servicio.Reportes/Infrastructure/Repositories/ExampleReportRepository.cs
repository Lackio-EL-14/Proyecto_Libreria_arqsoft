using System.Data;
using System.Data.Common;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.Exceptions;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Infrastructure.Repositories;

public sealed class ExampleReportRepository(IDbConnectionFactory connectionFactory) : IExampleReportRepository
{
    public async Task<IReadOnlyList<ReportRow>> ReadSampleAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TOP (@Limit) Nombre, PrecioVenta
                FROM dbo.Producto
                WHERE Estado = @Active
                ORDER BY Nombre, PublicId;
                """;
            AddParameter(command, "@Limit", DbType.Int32, 10);
            AddParameter(command, "@Active", DbType.Boolean, true);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<ReportRow>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new ReportRow(reader.GetString(0), reader.GetDecimal(1)));
            return rows;
        }
        catch (DbException exception)
        {
            throw new ReportDataUnavailableException("No se pudo consultar la información del reporte.", exception);
        }
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
