using System.Data;
using System.Data.Common;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Application.Exceptions;
using Libreria.Servicio.Reportes.Domain.Reports;

namespace Libreria.Servicio.Reportes.Infrastructure.Repositories;

public sealed class ProductReportRepository(IDbConnectionFactory connectionFactory) : IProductReportRepository
{
    public async Task<IReadOnlyList<ReportRow>> ReadProductsAsync(
        ProductReportFilter filter, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT p.Nombre, p.PrecioVenta, c.Nombre, m.Nombre
                FROM dbo.Producto p
                INNER JOIN dbo.Categoria c ON c.CategoriaId = p.CategoriaId
                INNER JOIN dbo.Marca m ON m.MarcaId = p.MarcaId
                WHERE p.Estado = @Active
                  AND (@CategoryPublicId IS NULL OR c.PublicId = @CategoryPublicId)
                  AND (@MinimumPrice IS NULL OR p.PrecioVenta >= @MinimumPrice)
                  AND (@MaximumPrice IS NULL OR p.PrecioVenta <= @MaximumPrice)
                ORDER BY p.Nombre, p.PublicId;
                """;
            AddParameter(command, "@Active", DbType.Boolean, true);
            AddParameter(command, "@CategoryPublicId", DbType.Guid, filter.CategoryPublicId);
            AddParameter(command, "@MinimumPrice", DbType.Decimal, filter.MinimumPrice);
            AddParameter(command, "@MaximumPrice", DbType.Decimal, filter.MaximumPrice);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<ReportRow>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(new(reader.GetString(0), reader.GetDecimal(1), reader.GetString(2), reader.GetString(3)));
            return rows;
        }
        catch (DbException exception)
        {
            throw new ReportDataUnavailableException("No se pudo consultar el reporte de productos.", exception);
        }
    }

    public async Task<IReadOnlyList<ReportCategory>> ReadCategoriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT PublicId, Nombre FROM dbo.Categoria
                WHERE Estado = @Active ORDER BY Nombre, PublicId;
                """;
            AddParameter(command, "@Active", DbType.Boolean, true);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var categories = new List<ReportCategory>();
            while (await reader.ReadAsync(cancellationToken))
                categories.Add(new(reader.GetGuid(0), reader.GetString(1)));
            return categories;
        }
        catch (DbException exception)
        {
            throw new ReportDataUnavailableException("No se pudieron consultar las categorías.", exception);
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
