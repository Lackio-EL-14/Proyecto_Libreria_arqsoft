using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Libreria.Web.Data;
using Libreria.Web.Pages.Categorias.Models;

namespace Libreria.Web.Pages.Categorias.Repositories
{
    public class CategoriaRepository : ICategoriaRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CategoriaRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<bool> ExisteNombreAsync(string nombre)
        {
            using var dbConnection = _connectionFactory.CreateConnection();
            
            if (dbConnection is not SqlConnection connection)
            {
                throw new InvalidOperationException("La conexión provista no es SqlConnection.");
            }

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM Categoria WHERE Nombre = @Nombre";
            command.Parameters.AddWithValue("@Nombre", nombre);

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            
            var count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task CrearAsync(Categoria categoria)
        {
            using var dbConnection = _connectionFactory.CreateConnection();
            
            if (dbConnection is not SqlConnection connection)
            {
                throw new InvalidOperationException("La conexión provista no es SqlConnection.");
            }

            using var command = connection.CreateCommand();
            
            command.CommandText = @"
                INSERT INTO Categoria (Nombre, Descripcion, Orden, Estado, FechaCreacion, FechaModificacion) 
                VALUES (@Nombre, @Descripcion, @Orden, 1, GETDATE(), GETDATE())";

            command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
            command.Parameters.AddWithValue("@Descripcion", string.IsNullOrEmpty(categoria.Descripcion) ? DBNull.Value : categoria.Descripcion);
            command.Parameters.AddWithValue("@Orden", categoria.Orden);

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            
            await command.ExecuteNonQueryAsync();
        }
        
        public async Task<IEnumerable<Categoria>> ObtenerTodasAsync(string? busqueda = null)
        {
            var categorias = new List<Categoria>();
            using var dbConnection = _connectionFactory.CreateConnection();
            
            if (dbConnection is not SqlConnection connection)
            {
                throw new InvalidOperationException("La conexión provista no es SqlConnection.");
            }

            using var command = connection.CreateCommand();
            
            var query = "SELECT CategoriaId, Nombre, Descripcion, Orden, Estado, FechaCreacion, FechaModificacion FROM Categoria";
            
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query += " WHERE Nombre LIKE @Busqueda";
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda}%");
            }

            query += " ORDER BY Nombre ASC";
            command.CommandText = query;

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categorias.Add(new Categoria
                {
                    CategoriaId = reader.GetInt32(reader.GetOrdinal("CategoriaId")),
                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                    Descripcion = reader.IsDBNull(reader.GetOrdinal("Descripcion")) ? null : reader.GetString(reader.GetOrdinal("Descripcion")),
                    Orden = reader.GetInt32(reader.GetOrdinal("Orden")),
                    Estado = reader.GetBoolean(reader.GetOrdinal("Estado")),
                    // Validación de nulos para las fechas
                    FechaCreacion = reader.IsDBNull(reader.GetOrdinal("FechaCreacion")) 
                        ? DateTime.MinValue 
                        : reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                    FechaModificacion = reader.IsDBNull(reader.GetOrdinal("FechaModificacion")) 
                        ? DateTime.MinValue 
                        : reader.GetDateTime(reader.GetOrdinal("FechaModificacion"))
                });
            }

            return categorias;
        }

    }
}
