using System.Data;
using System.Data.Common;
using Libreria.Web.Data;
using Libreria.Web.Pages.Productos.Models;

namespace Libreria.Web.Pages.Productos.Repositories;

public class CatalogoProductoRepository :
    IConsultaCatalogoProductoRepository,
    IValidacionCatalogoProductoRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CatalogoProductoRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public IReadOnlyList<CategoriaOption> ObtenerCategoriasActivas()
    {
        var categorias = new List<CategoriaOption>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT CategoriaId, Nombre
            FROM Categoria
            WHERE Estado = 1
            ORDER BY Nombre";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categorias.Add(new CategoriaOption(reader.GetInt32(0), reader.GetString(1)));
        }
        return categorias;
    }

    public IReadOnlyList<MarcaOption> ObtenerMarcasActivas()
    {
        var marcas = new List<MarcaOption>();
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT MarcaId, Nombre
            FROM Marca
            WHERE Estado = 1
            ORDER BY Nombre";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            marcas.Add(new MarcaOption(reader.GetInt32(0), reader.GetString(1)));
        }
        return marcas;
    }

    public bool ExisteCategoriaActiva(int categoriaId)
    {
        return ExisteActivo("Categoria", "CategoriaId", categoriaId);
    }

    public bool ExisteMarcaActiva(int marcaId)
    {
        return ExisteActivo("Marca", "MarcaId", marcaId);
    }

    private bool ExisteActivo(string tabla, string columnaId, int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT COUNT(1)
            FROM {tabla}
            WHERE {columnaId} = @Id
              AND Estado = 1";
        AgregarParametro(command, "@Id", id);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void AgregarParametro(DbCommand command, string nombre, object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        parametro.DbType = DbType.Int32;
        command.Parameters.Add(parametro);
    }
}
