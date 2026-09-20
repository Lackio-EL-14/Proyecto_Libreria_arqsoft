using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Libreria.Web.Data;
using Libreria.Web.Domain.Entities; 

namespace Libreria.Web.Data.Repositories
{
    public class CategoriaRepository : ICrudRepository<Categoria>
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CategoriaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<Categoria>> ObtenerActivasAsync(string? busqueda = null)
        {
            var categorias = new List<Categoria>();
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();
            
            command.CommandText = @"
                SELECT CategoriaId, PublicId, Codigo, Nombre, Descripcion, Ubicacion,
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

        public async Task<Categoria?> ObtenerPorPublicIdAsync(Guid publicId, bool estadoEsperado)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();
            
            command.CommandText = @"
                SELECT CategoriaId, PublicId, Codigo, Nombre, Descripcion, Ubicacion,
                       Estado, FechaCreacion, FechaModificacion
                FROM Categoria
                WHERE PublicId = @PublicId
                  AND Estado = @Estado";
                  
            AgregarParametro(command, "@PublicId", publicId);
            AgregarParametro(command, "@Estado", estadoEsperado);
            
            await using var reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapearCategoria(reader) : null;
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
                WHERE PublicId = @PublicId
                  AND Estado = 1";
                  
            AgregarDatosCategoria(command, categoria);
            AgregarParametro(command, "@PublicId", categoria.PublicId);
            
            return await command.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> TieneRelacionesAsync(Guid publicId)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();
            
            command.CommandText = @"
                SELECT COUNT(1)
                FROM Producto p
                INNER JOIN Categoria c ON p.CategoriaId = c.CategoriaId
                WHERE c.PublicId = @PublicId
                  AND p.Estado = 1";
                  
            AgregarParametro(command, "@PublicId", publicId);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        public async Task<bool> CambiarEstadoAsync(Guid publicId, bool nuevoEstado)
        {
            bool estadoEsperado = !nuevoEstado; 
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();
            
            command.CommandText = @"
                UPDATE Categoria
                SET Estado = @NuevoEstado,
                    FechaModificacion = SYSDATETIME()
                WHERE PublicId = @PublicId
                  AND Estado = @EstadoEsperado";
                  
            AgregarParametro(command, "@PublicId", publicId);
            AgregarParametro(command, "@NuevoEstado", nuevoEstado);
            AgregarParametro(command, "@EstadoEsperado", estadoEsperado);
            
            return await command.ExecuteNonQueryAsync() == 1;
        }

        public async Task<bool> ExisteValorAsync(string columna, string valor, Guid? excluirPublicId = null)
        {
            await using var connection = await CrearConexionAbiertaAsync();
            await using var command = connection.CreateCommand();
            
            command.CommandText = $@"
                SELECT COUNT(1)
                FROM Categoria
                WHERE {columna} = @Valor
                  AND (@ExcluirId IS NULL OR PublicId <> @ExcluirId)";
                  
            AgregarParametro(command, "@Valor", valor);
            AgregarParametro(command, "@ExcluirId", excluirPublicId ?? (object)DBNull.Value);
            
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
                PublicId = reader.GetGuid(reader.GetOrdinal("PublicId")), 
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
}
