using System.Data;
using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class IndexModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IndexModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<MarcaListItem> Marcas { get; private set; } = new();

    public string? NombreBusqueda { get; private set; }

    public void OnGet(string? nombre)
    {
        NombreBusqueda = nombre?.Trim();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                MarcaId,
                Nombre,
                Descripcion,
                PaisOrigen,
                Estado
            FROM Marca
            WHERE (@Nombre IS NULL OR Nombre LIKE '%' + @Nombre + '%')
            ORDER BY Nombre ASC";

        var nombreParameter = command.CreateParameter();

        nombreParameter.ParameterName = "@Nombre";
        nombreParameter.DbType = DbType.String;
        nombreParameter.Size = 100;

        nombreParameter.Value =
            string.IsNullOrWhiteSpace(NombreBusqueda)
                ? DBNull.Value
                : NombreBusqueda;

        command.Parameters.Add(nombreParameter);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            Marcas.Add(new MarcaListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetBoolean(4)
            ));
        }
    }
}

public record MarcaListItem(
    int MarcaId,
    string Nombre,
    string? Descripcion,
    string? PaisOrigen,
    bool Estado
);