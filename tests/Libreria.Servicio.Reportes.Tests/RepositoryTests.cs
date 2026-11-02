using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Libreria.Servicio.Reportes.Application.Abstractions;
using Libreria.Servicio.Reportes.Application.DTOs;
using Libreria.Servicio.Reportes.Infrastructure.Connection;
using Libreria.Servicio.Reportes.Infrastructure.Repositories;
using Microsoft.Data.SqlClient;

namespace Libreria.Servicio.Reportes.Tests;

public sealed class RepositoryTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ProductsQueryJoinsTablesAndBindsOptionalFilters(bool category, bool prices)
    {
        var connection = new RecordingConnection();
        var filter = new ProductReportFilter { CategoryPublicId = category ? ProductSampleRepository.CategoryId : null,
            MinimumPrice = prices ? 10.25m : null, MaximumPrice = prices ? 20m : null };
        var rows = await new ProductReportRepository(new RecordingConnectionFactory(connection))
            .ReadProductsAsync(filter, CancellationToken.None);
        var sql = connection.Command.Sql;
        Assert.Contains("INNER JOIN dbo.Categoria c ON c.CategoriaId = p.CategoriaId", sql);
        Assert.Contains("INNER JOIN dbo.Marca m ON m.MarcaId = p.MarcaId", sql);
        Assert.Contains("ORDER BY p.Nombre, p.PublicId", sql);
        Assert.Contains("p.PrecioVenta >= @MinimumPrice", sql);
        Assert.Contains("p.PrecioVenta <= @MaximumPrice", sql);
        Assert.Contains("c.PublicId = @CategoryPublicId", sql);
        Assert.False(Regex.IsMatch(sql, @"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|EXEC|TRUNCATE|Usuario|Rol)\b", RegexOptions.IgnoreCase));
        Assert.Equal((object?)filter.CategoryPublicId ?? DBNull.Value, connection.Command.Arguments["@CategoryPublicId"].Item2);
        Assert.Equal((object?)filter.MinimumPrice ?? DBNull.Value, connection.Command.Arguments["@MinimumPrice"].Item2);
        Assert.Equal((object?)filter.MaximumPrice ?? DBNull.Value, connection.Command.Arguments["@MaximumPrice"].Item2);
        Assert.Equal("Papelería", Assert.Single(rows).Category);
        Assert.Equal("Escolar", rows[0].Brand);
    }

    [ReadOnlySqlFact]
    public async Task ProductReportSupportsRealReadOnlyQueryWhenConfigured()
    {
        var strings = ConnectionStringSingleton.Instancia;
        strings.Configurar(Environment.GetEnvironmentVariable("LIBRERIA_REPORTES_SQL_TEST_CONNECTION")!);
        var repository = new ProductReportRepository(new SqlConnectionFactory(strings));
        var categories = await repository.ReadCategoriesAsync(CancellationToken.None);
        var filter = new ProductReportFilter { CategoryPublicId = categories.FirstOrDefault()?.PublicId, MinimumPrice = 0 };
        var rows = await repository.ReadProductsAsync(filter, CancellationToken.None);
        Assert.All(rows, row => Assert.True(row.Amount >= 0 && row.Category is not null && row.Brand is not null));
    }

    [Fact]
    public async Task AdoRepositoryExecutesOnlyParameterizedSelectAndMapsDecimals()
    {
        var connection = new RecordingConnection();
        var repository = new ExampleReportRepository(new RecordingConnectionFactory(connection));
        var rows = await repository.ReadSampleAsync(CancellationToken.None);

        Assert.Single(rows);
        Assert.Equal("Cuaderno", rows[0].Name);
        Assert.Equal(12.50m, rows[0].Amount);
        Assert.StartsWith("SELECT", connection.Command.Sql.TrimStart());
        Assert.False(Regex.IsMatch(connection.Command.Sql,
            @"\b(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|EXEC|TRUNCATE|Usuario|Rol)\b", RegexOptions.IgnoreCase));
        Assert.Contains("TOP (@Limit)", connection.Command.Sql);
        Assert.Contains("Estado = @Active", connection.Command.Sql);
        Assert.Equal((DbType.Int32, (object)10), connection.Command.Arguments["@Limit"]);
        Assert.Equal((DbType.Boolean, (object)true), connection.Command.Arguments["@Active"]);
        Assert.True(connection.WasDisposed);
    }

    [Fact]
    public async Task CancellationPropagatesBeforeReading()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new ExampleReportRepository(new RecordingConnectionFactory(new RecordingConnection()));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.ReadSampleAsync(cancellation.Token));
    }

    [ReadOnlySqlFact]
    public async Task ConfiguredDatabaseSupportsRealReadOnlyQuery()
    {
        // Exclusivamente SELECT sobre una base existente. No crea ni modifica datos o esquema.
        var connectionStrings = ConnectionStringSingleton.Instancia;
        connectionStrings.Configurar(Environment.GetEnvironmentVariable("LIBRERIA_REPORTES_SQL_TEST_CONNECTION")!);
        var repository = new ExampleReportRepository(new SqlConnectionFactory(connectionStrings));
        var rows = await repository.ReadSampleAsync(CancellationToken.None);
        Assert.InRange(rows.Count, 0, 10);
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Name)));
    }

    private sealed class RecordingConnectionFactory(RecordingConnection connection) : IDbConnectionFactory
    {
        public Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<DbConnection>(connection);
        }
    }

    private sealed class RecordingConnection : DbConnection
    {
        internal RecordingCommand Command { get; } = new();
        internal bool WasDisposed { get; private set; }
        [AllowNull] public override string ConnectionString { get; set; } = "";
        public override string Database => "Test";
        public override string DataSource => "Test";
        public override string ServerVersion => "Test";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => Command;
        protected override void Dispose(bool disposing) { WasDisposed = true; base.Dispose(disposing); }
    }

    private sealed class RecordingCommand : DbCommand
    {
        private readonly SqlCommand _parameterContainer = new();
        internal string Sql { get; private set; } = "";
        internal Dictionary<string, (DbType, object)> Arguments { get; } = [];
        [AllowNull] public override string CommandText { get; set; } = "";
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbTransaction? DbTransaction { get; set; }
        protected override DbParameterCollection DbParameterCollection => _parameterContainer.Parameters;
        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new InvalidOperationException("No se permiten escrituras.");
        public override object ExecuteScalar() => throw new NotSupportedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new SqlParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            Sql = CommandText;
            foreach (DbParameter parameter in Parameters)
                Arguments.Add(parameter.ParameterName, (parameter.DbType, parameter.Value!));
            var table = new DataTable();
            table.Columns.Add("Nombre", typeof(string));
            table.Columns.Add("PrecioVenta", typeof(decimal));
            if (Sql.Contains("INNER JOIN"))
            {
                table.Columns.Add("Categoria", typeof(string));
                table.Columns.Add("Marca", typeof(string));
                table.Rows.Add("Cuaderno", 12.50m, "Papelería", "Escolar");
            }
            else table.Rows.Add("Cuaderno", 12.50m);
            return table.CreateDataReader();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _parameterContainer.Dispose();
            base.Dispose(disposing);
        }
    }
}

public sealed class ReadOnlySqlFactAttribute : FactAttribute
{
    public ReadOnlySqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("LIBRERIA_REPORTES_SQL_TEST_CONNECTION")))
            Skip = "Falta LIBRERIA_REPORTES_SQL_TEST_CONNECTION para una BD existente; no se crean datos.";
    }
}
