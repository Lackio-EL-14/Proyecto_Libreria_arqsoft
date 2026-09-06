using System.Data;
using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Marcas;

public class DeactivateModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DeactivateModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public MarcaBajaView Marca { get; private set; } = new();

    [BindProperty]
    public int MarcaId { get; set; }

    public IActionResult OnGet(int id)
    {
        using var connection = _connectionFactory.CreateConnection();

        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                MarcaId,
                Nombre,
                Descripcion,
                PaisOrigen,
                Estado
            FROM dbo.Marca
            WHERE MarcaId = @MarcaId;";

        AgregarParametroEntero(command, "@MarcaId", id);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return NotFound();
        }

        Marca = new MarcaBajaView
        {
            MarcaId = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            Descripcion = reader.IsDBNull(2)
                ? null
                : reader.GetString(2),
            PaisOrigen = reader.IsDBNull(3)
                ? null
                : reader.GetString(3),
            Estado = reader.GetBoolean(4)
        };

        MarcaId = Marca.MarcaId;

        reader.Close();

        Marca.ProductosActivos = ContarProductosActivos(
            connection,
            Marca.MarcaId);

        return Page();
    }

    public IActionResult OnPost()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE dbo.Marca
            SET
                Estado = 0,
                FechaModificacion = SYSDATETIME()
            WHERE MarcaId = @MarcaId
              AND Estado = 1;";

        AgregarParametroEntero(
            command,
            "@MarcaId",
            MarcaId);

        command.ExecuteNonQuery();

        return RedirectToPage("./Index");
    }

    private static int ContarProductosActivos(
        IDbConnection connection,
        int marcaId)
    {
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT COUNT(1)
            FROM dbo.Producto
            WHERE MarcaId = @MarcaId
              AND Estado = 1;";

        AgregarParametroEntero(
            command,
            "@MarcaId",
            marcaId);

        var resultado = command.ExecuteScalar();

        return Convert.ToInt32(resultado);
    }

    private static void AgregarParametroEntero(
        IDbCommand command,
        string nombre,
        int valor)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = nombre;
        parameter.DbType = DbType.Int32;
        parameter.Value = valor;

        command.Parameters.Add(parameter);
    }
}

public class MarcaBajaView
{
    public int MarcaId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public string? PaisOrigen { get; set; }

    public bool Estado { get; set; }

    public int ProductosActivos { get; set; }
}