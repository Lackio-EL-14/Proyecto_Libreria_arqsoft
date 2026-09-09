using System.Data;
using System.Data.Common;
using Libreria.Web.Data;
using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories;

public class CategoriaRepository : ICategoriaRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CategoriaRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(string? busqueda)
    {
        var categorias = new List<Categoria>();
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT CategoriaId, Codigo, Nombre, Descripcion, Ubicacion,
                   Estado, FechaCreacion, FechaModificacion
            FROM Categoria
            WHERE Estado = 1
              AND (@Busqueda IS NULL
                   OR Codigo LIKE '%' + @Busqueda + '%'
                   OR Nombre LIKE '%' + @Busqueda + '%'
                   OR Ubicacion LIKE '%' + @Busqueda + '%')
            ORDER BY Nombre";
        AgregarParametro(command, "@Busqueda",
            string.IsNullOrWhiteSpace(busqueda) ? DBNull.Value : busqueda.Trim());

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categorias.Add(MapearCategoria(reader));
        }

        return categorias;
    }

    public async Task CrearAsync(Categoria categoria)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Categoria
                (Codigo, Nombre, Descripcion, Ubicacion, Estado,
                 FechaCreacion, FechaModificacion)
            VALUES
                (@Codigo, @Nombre, @Descripcion, @Ubicacion, 1,
                 SYSDATETIME(), SYSDATETIME())";
        AgregarDatosCategoria(command, categoria);
        await command.ExecuteNonQueryAsync();
    }

    public Task<Categoria?> ObtenerActivaPorIdAsync(int categoriaId)
    {
        return ObtenerPorEstadoAsync(categoriaId, true);
    }

    public Task<Categoria?> ObtenerInactivaPorIdAsync(int categoriaId)
    {
        return ObtenerPorEstadoAsync(categoriaId, false);
    }

    public async Task<bool> ActualizarAsync(Categoria categoria)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Categoria
            SET Codigo = @Codigo,
                Nombre = @Nombre,
                Descripcion = @Descripcion,
                Ubicacion = @Ubicacion,
                FechaModificacion = SYSDATETIME()
            WHERE CategoriaId = @CategoriaId
              AND Estado = 1";
        AgregarDatosCategoria(command, categoria);
        AgregarParametro(command, "@CategoriaId", categoria.CategoriaId);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> TieneProductosActivosAsync(int categoriaId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT COUNT(1)
            FROM Producto
            WHERE CategoriaId = @CategoriaId
              AND Estado = 1";
        AgregarParametro(command, "@CategoriaId", categoriaId);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    public Task<bool> DarDeBajaAsync(int categoriaId)
    {
        return CambiarEstadoAsync(categoriaId, false, true);
    }

    public Task<bool> ReactivarAsync(int categoriaId)
    {
        return CambiarEstadoAsync(categoriaId, true, false);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId)
    {
        return ExisteValorAsync("Codigo", codigo, excluirId);
    }

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId)
    {
        return ExisteValorAsync("Nombre", nombre, excluirId);
    }

    private async Task<Categoria?> ObtenerPorEstadoAsync(int categoriaId, bool estado)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT CategoriaId, Codigo, Nombre, Descripcion, Ubicacion,
                   Estado, FechaCreacion, FechaModificacion
            FROM Categoria
            WHERE CategoriaId = @CategoriaId
              AND Estado = @Estado";
        AgregarParametro(command, "@CategoriaId", categoriaId);
        AgregarParametro(command, "@Estado", estado);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapearCategoria(reader) : null;
    }

    private async Task<bool> CambiarEstadoAsync(
        int categoriaId,
        bool nuevoEstado,
        bool estadoEsperado)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE Categoria
            SET Estado = @NuevoEstado,
                FechaModificacion = SYSDATETIME()
            WHERE CategoriaId = @CategoriaId
              AND Estado = @EstadoEsperado";
        AgregarParametro(command, "@CategoriaId", categoriaId);
        AgregarParametro(command, "@NuevoEstado", nuevoEstado);
        AgregarParametro(command, "@EstadoEsperado", estadoEsperado);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private async Task<bool> ExisteValorAsync(
        string columna,
        string valor,
        int? excluirId)
    {
        await using var connection = await CrearConexionAbiertaAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT COUNT(1)
            FROM Categoria
            WHERE {columna} = @Valor
              AND (@ExcluirId IS NULL OR CategoriaId <> @ExcluirId)";
        AgregarParametro(command, "@Valor", valor);
        AgregarParametro(command, "@ExcluirId", excluirId ?? (object)DBNull.Value);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
    }

    private async Task<DbConnection> CrearConexionAbiertaAsync()
    {
        var connection = _connectionFactory.CreateConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        return connection;
    }

    private static Categoria MapearCategoria(DbDataReader reader)
    {
        return new Categoria
        {
            CategoriaId = reader.GetInt32(reader.GetOrdinal("CategoriaId")),
            Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
            Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
            Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion"))
                ? null
                : reader.GetString(reader.GetOrdinal("Descripcion")),
            Ubicacion = reader.GetString(reader.GetOrdinal("Ubicacion")),
            Estado = reader.GetBoolean(reader.GetOrdinal("Estado")),
            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
            FechaModificacion = reader.IsDBNull(reader.GetOrdinal("FechaModificacion"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("FechaModificacion"))
        };
    }

    private static void AgregarDatosCategoria(DbCommand command, Categoria categoria)
    {
        AgregarParametro(command, "@Codigo", categoria.Codigo);
        AgregarParametro(command, "@Nombre", categoria.Nombre);
        AgregarParametro(command, "@Descripcion", categoria.Descripcion ?? (object)DBNull.Value);
        AgregarParametro(command, "@Ubicacion", categoria.Ubicacion);
    }

    private static void AgregarParametro(DbCommand command, string nombre, object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }
}
