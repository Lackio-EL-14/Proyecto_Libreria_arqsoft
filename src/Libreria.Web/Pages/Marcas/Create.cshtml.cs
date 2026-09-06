using System.ComponentModel.DataAnnotations;
using System.Data;
using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace Libreria.Web.Pages.Marcas;

public class CreateModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CreateModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [BindProperty]
    public MarcaInput Input { get; set; } = new();

    public void OnGet()
    {
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

        if (ExisteMarcaConNombre(connection, Input.Nombre))
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "Ya existe una marca registrada con ese nombre.");

            return Page();
        }

        using var command = connection.CreateCommand();

        command.CommandText = @"
            INSERT INTO dbo.Marca
                (Nombre, Descripcion, PaisOrigen)
            VALUES
                (@Nombre, @Descripcion, @PaisOrigen);";

        AgregarParametro(
            command,
            "@Nombre",
            Input.Nombre,
            100);

        AgregarParametro(
            command,
            "@Descripcion",
            Input.Descripcion,
            255);

        AgregarParametro(
            command,
            "@PaisOrigen",
            Input.PaisOrigen,
            100);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            ModelState.AddModelError(
                "Input.Nombre",
                "Ya existe una marca registrada con ese nombre.");

            return Page();
        }

        return RedirectToPage("./Index");
    }

    private static bool ExisteMarcaConNombre(
        IDbConnection connection,
        string nombre)
    {
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT COUNT(1)
            FROM dbo.Marca
            WHERE Nombre = @Nombre;";

        AgregarParametro(
            command,
            "@Nombre",
            nombre,
            100);

        var resultado = command.ExecuteScalar();

        return Convert.ToInt32(resultado) > 0;
    }

    private static void AgregarParametro(
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

    private static string? NormalizarCampoOpcional(string? valor)
    {
        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim();
    }
}

public class MarcaInput
{
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