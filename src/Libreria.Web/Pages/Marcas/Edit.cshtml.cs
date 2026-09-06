using System.ComponentModel.DataAnnotations;
using System.Data;
using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Libreria.Web.Pages.Marcas;

public class EditModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public EditModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [BindProperty]
    public MarcaEditInput Input { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT
                MarcaId,
                Nombre,
                Descripcion,
                PaisOrigen
            FROM dbo.Marca
            WHERE MarcaId = @MarcaId;";

        AgregarParametroEntero(command, "@MarcaId", id);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return NotFound();
        }

        Input = new MarcaEditInput
        {
            MarcaId = reader.GetInt32(0),
            Nombre = reader.GetString(1),
            Descripcion = reader.IsDBNull(2)
                ? null
                : reader.GetString(2),
            PaisOrigen = reader.IsDBNull(3)
                ? null
                : reader.GetString(3)
        };

        return Page();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Input.Nombre = Input.Nombre.Trim();
        Input.Descripcion = NormalizarCampoOpcional(Input.Descripcion);
        Input.PaisOrigen = NormalizarCampoOpcional(Input.PaisOrigen);

        using var connection = _connectionFactory.CreateConnection();

        if (ExisteOtraMarcaConNombre(
            connection,
            Input.MarcaId,
            Input.Nombre))
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "Ya existe otra marca registrada con ese nombre.");

            return Page();
        }

        using var command = connection.CreateCommand();

        command.CommandText = @"
            UPDATE dbo.Marca
            SET
                Nombre = @Nombre,
                Descripcion = @Descripcion,
                PaisOrigen = @PaisOrigen,
                FechaModificacion = SYSDATETIME()
            WHERE MarcaId = @MarcaId;";

        AgregarParametroTexto(
            command,
            "@Nombre",
            Input.Nombre,
            100);

        AgregarParametroTexto(
            command,
            "@Descripcion",
            Input.Descripcion,
            255);

        AgregarParametroTexto(
            command,
            "@PaisOrigen",
            Input.PaisOrigen,
            100);

        AgregarParametroEntero(
            command,
            "@MarcaId",
            Input.MarcaId);

        try
        {
            var filasAfectadas = command.ExecuteNonQuery();

            if (filasAfectadas == 0)
            {
                return NotFound();
            }
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "Ya existe otra marca registrada con ese nombre.");

            return Page();
        }

        return RedirectToPage("./Index");
    }

    private static bool ExisteOtraMarcaConNombre(
        IDbConnection connection,
        int marcaId,
        string nombre)
    {
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT COUNT(1)
            FROM dbo.Marca
            WHERE Nombre = @Nombre
              AND MarcaId <> @MarcaId;";

        AgregarParametroTexto(
            command,
            "@Nombre",
            nombre,
            100);

        AgregarParametroEntero(
            command,
            "@MarcaId",
            marcaId);

        var resultado = command.ExecuteScalar();

        return Convert.ToInt32(resultado) > 0;
    }

    private static void AgregarParametroTexto(
        IDbCommand command,
        string nombre,
        string? valor,
        int tamanio)
    {
        var parameter = command.CreateParameter();

        parameter.ParameterName = nombre;
        parameter.DbType = DbType.String;
        parameter.Size = tamanio;
        parameter.Value = string.IsNullOrWhiteSpace(valor)
            ? DBNull.Value
            : valor;

        command.Parameters.Add(parameter);
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

    private static string? NormalizarCampoOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }
}

public class MarcaEditInput
{
    public int MarcaId { get; set; }

    [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
    [StringLength(
        100,
        ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(
        255,
        ErrorMessage = "La descripción no puede superar los 255 caracteres.")]
    public string? Descripcion { get; set; }

    [Display(Name = "País de origen")]
    [StringLength(
        100,
        ErrorMessage = "El país de origen no puede superar los 100 caracteres.")]
    public string? PaisOrigen { get; set; }
}