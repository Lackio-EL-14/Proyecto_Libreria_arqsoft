using System;
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

            await connection.OpenAsync();
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

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
    }
}
