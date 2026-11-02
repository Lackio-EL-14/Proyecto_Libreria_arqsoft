using System.Data;
using System.Text.RegularExpressions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Infrastructure.Connection;
using Libreria.Servicio.Reportes.Infrastructure.Repositories;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class SalesReportRepositoryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryUsesRealJoinsActiveSalesGroupingSumsAndTypedOptionalParameters(bool filtered)
    {
        var connection = new RepositoryTests.RecordingConnection();
        var filter = new SalesReportFilter
        {
            DateFrom = filtered ? new DateOnly(2026, 11, 1) : null,
            DateTo = filtered ? new DateOnly(2026, 11, 2) : null,
            CategoryPublicId = filtered ? ProductSampleRepository.CategoryId : null
        };
        var rows = await new SalesReportRepository(new RepositoryTests.RecordingConnectionFactory(connection))
            .ReadSalesAsync(filter, CancellationToken.None);
        var sql = connection.Command.Sql;
        foreach (var fragment in new[]
        {
            "FROM dbo.Venta v", "INNER JOIN dbo.DetalleVenta d ON d.VentaId = v.VentaId",
            "INNER JOIN dbo.Producto p ON p.ProductoId = d.ProductoId",
            "INNER JOIN dbo.Categoria c ON c.CategoriaId = p.CategoriaId",
            "v.Estado = @ActiveStatus", "v.FechaCreacion >= @DateFrom", "v.FechaCreacion <= @DateTo",
            "c.PublicId = @CategoryPublicId", "SUM(CAST(d.Cantidad AS bigint))", "SUM(d.Importe)",
            "GROUP BY p.ProductoId, p.Nombre, c.CategoriaId, c.Nombre", "ORDER BY p.Nombre, c.Nombre, p.ProductoId"
        }) Assert.Contains(fragment, sql);
        Assert.False(Regex.IsMatch(sql, @"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|EXEC|TRUNCATE)\b", RegexOptions.IgnoreCase));
        Assert.DoesNotContain("SUM(v.Total)", sql);
        Assert.Equal((DbType.String, (object)"Activa"), connection.Command.Arguments["@ActiveStatus"]);
        Assert.Equal((DbType.DateTime2, (object?)filter.DateFrom?.ToDateTime(TimeOnly.MinValue) ?? DBNull.Value),
            connection.Command.Arguments["@DateFrom"]);
        Assert.Equal((DbType.DateTime2, (object?)filter.DateTo?.ToDateTime(TimeOnly.MaxValue) ?? DBNull.Value),
            connection.Command.Arguments["@DateTo"]);
        Assert.Equal((DbType.Guid, (object?)filter.CategoryPublicId ?? DBNull.Value), connection.Command.Arguments["@CategoryPublicId"]);
        Assert.Equal(5, Assert.Single(rows).Quantity);
        Assert.Equal(65m, rows[0].Amount);
        Assert.True(connection.WasDisposed);
    }

    [ReadOnlySqlFact]
    public async Task RealSalesQueryCanBeExecutedWhenExistingSqlServerIsConfigured()
    {
        var strings = ConnectionStringSingleton.Instancia;
        strings.Configurar(Environment.GetEnvironmentVariable("LIBRERIA_REPORTES_SQL_TEST_CONNECTION")!);
        var rows = await new SalesReportRepository(new SqlConnectionFactory(strings))
            .ReadSalesAsync(new SalesReportFilter(), CancellationToken.None);
        Assert.All(rows, row => { Assert.True(row.Quantity > 0); Assert.True(row.Amount >= 0); });
    }
}
