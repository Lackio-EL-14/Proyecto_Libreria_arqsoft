using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Categorias;

public class IndexModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IndexModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<CategoriaListItem> Categorias { get; private set; } = new();

    public void OnGet()
    {
        // EJEMPLO DE REFERENCIA para todo el equipo: así se hace una
        // consulta con ADO.NET puro (sin Entity Framework). El resto del
        // CRUD (Insert/Update/Delete) de cualquier tabla sigue el mismo
        // patrón: pedir la conexión a la fábrica, crear un SqlCommand,
        // PARAMETRIZAR siempre (nunca concatenar strings, por SQL Injection)
        // y ejecutar.
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT CategoriaId, Nombre, Descripcion, Estado
            FROM Categoria
            WHERE Estado = 1
            ORDER BY Nombre";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            Categorias.Add(new CategoriaListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.GetBoolean(3)));
        }
    }
}

public record CategoriaListItem(int CategoriaId, string Nombre, string? Descripcion, bool Estado);
