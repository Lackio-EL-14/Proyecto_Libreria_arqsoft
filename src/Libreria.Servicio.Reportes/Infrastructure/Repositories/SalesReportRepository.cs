using System.Data;
using System.Data.Common;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Exceptions;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Infrastructure.Repositories;

public sealed class SalesReportRepository(IDbConnectionFactory connectionFactory) : ISalesReportRepository
{
    public async Task<IReadOnlyList<SalesReportRow>> ReadSalesAsync(SalesReportFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT p.Nombre, c.Nombre,
                       SUM(CAST(d.Cantidad AS bigint)) AS Quantity, SUM(d.Importe) AS Amount
                FROM dbo.Venta v
                INNER JOIN dbo.DetalleVenta d ON d.VentaId = v.VentaId
                INNER JOIN dbo.Producto p ON p.ProductoId = d.ProductoId
                INNER JOIN dbo.Categoria c ON c.CategoriaId = p.CategoriaId
                WHERE v.Estado = @ActiveStatus
                  AND (@DateFrom IS NULL OR v.FechaCreacion >= @DateFrom)
                  AND (@DateTo IS NULL OR v.FechaCreacion <= @DateTo)
                  AND (@CategoryPublicId IS NULL OR c.PublicId = @CategoryPublicId)
                GROUP BY p.ProductoId, p.Nombre, c.CategoriaId, c.Nombre
                ORDER BY p.Nombre, c.Nombre, p.ProductoId;
                """;
            AddParameter(command, "@ActiveStatus", DbType.String, "Activa");
            AddParameter(command, "@DateFrom", DbType.DateTime2, filter.DateFrom?.ToDateTime(TimeOnly.MinValue));
            AddParameter(command, "@DateTo", DbType.DateTime2, filter.DateTo?.ToDateTime(TimeOnly.MaxValue));
            AddParameter(command, "@CategoryPublicId", DbType.Guid, filter.CategoryPublicId);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<SalesReportRow>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new(reader.GetString(0), reader.GetString(1), reader.GetInt64(2), reader.GetDecimal(3)));
            return rows;
        }
        catch (DbException exception)
        {
            throw new ReportDataUnavailableException("No se pudo consultar el reporte de ventas.", exception);
        }
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
