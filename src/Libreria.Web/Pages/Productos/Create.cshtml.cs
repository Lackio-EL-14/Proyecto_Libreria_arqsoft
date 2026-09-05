using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace Libreria.Web.Pages.Productos;

public class CreateModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CreateModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    [BindProperty]
    public ProductoInput Input { get; set; } = new();

    public List<CategoriaOption> Categorias { get; private set; } = new();
    public List<MarcaOption> Marcas { get; private set; } = new();

    public void OnGet()
    {
        CargarCategorias();
        CargarMarcas();

    }

    public IActionResult OnPost()
    {
        if (Input.FechaVencimiento.HasValue &&
            Input.FechaVencimiento.Value.Date < DateTime.Today)
        {
            ModelState.AddModelError(
                "Input.FechaVencimiento",
                "La fecha de vencimiento no puede estar en el pasado.");
        }

        if (Input.CategoriaId > 0 && !ExisteCategoriaActiva(Input.CategoriaId))
        {
            ModelState.AddModelError(
                "Input.CategoriaId",
                "La categoría seleccionada no está disponible.");
        }

        if (Input.MarcaId > 0 && !ExisteMarcaActiva(Input.MarcaId))
        {
            ModelState.AddModelError(
                "Input.MarcaId",
                "La marca seleccionada no está disponible.");
        }


        CargarCategorias();
        CargarMarcas();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        RegistrarProductoConHistorico();

        TempData["MensajeExito"] = "Producto registrado correctamente.";

        return RedirectToPage("Index");
    }

    private void CargarCategorias()
    {
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
            Categorias.Add(new CategoriaOption(
                reader.GetInt32(0),
                reader.GetString(1)
            ));
        }
    }

    private void CargarMarcas()
    {
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
            Marcas.Add(new MarcaOption(
                reader.GetInt32(0),
                reader.GetString(1)
            ));
        }
    }

    private bool ExisteCategoriaActiva(int categoriaId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT COUNT(*)
        FROM Categoria
        WHERE CategoriaId = @CategoriaId
          AND Estado = 1";

        var parametro = command.CreateParameter();
        parametro.ParameterName = "@CategoriaId";
        parametro.Value = categoriaId;
        command.Parameters.Add(parametro);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private bool ExisteMarcaActiva(int marcaId)
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT COUNT(*)
        FROM Marca
        WHERE MarcaId = @MarcaId
          AND Estado = 1";

        var parametro = command.CreateParameter();
        parametro.ParameterName = "@MarcaId";
        parametro.Value = marcaId;
        command.Parameters.Add(parametro);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private int RegistrarProducto(
        IDbConnection connection,
        IDbTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = @"
        INSERT INTO Producto
        (
            Nombre,
            DescripcionEspecifica,
            FechaVencimiento,
            Stock,
            PrecioVenta,
            CostoAdquisicionActual,
            CategoriaId,
            MarcaId,
            Estado
        )
        VALUES
        (
            @Nombre,
            @DescripcionEspecifica,
            @FechaVencimiento,
            @Stock,
            @PrecioVenta,
            @CostoAdquisicionActual,
            @CategoriaId,
            @MarcaId,
            1
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        AgregarParametro(command, "@Nombre", Input.Nombre.Trim());
        AgregarParametro(command, "@DescripcionEspecifica",
            string.IsNullOrWhiteSpace(Input.DescripcionEspecifica)
                ? DBNull.Value
                : Input.DescripcionEspecifica.Trim());

        AgregarParametro(command, "@FechaVencimiento",
            Input.FechaVencimiento.HasValue
                ? Input.FechaVencimiento.Value.Date
                : DBNull.Value);

        AgregarParametro(command, "@Stock", Input.Stock);
        AgregarParametro(command, "@PrecioVenta", Input.PrecioVenta);
        AgregarParametro(command, "@CostoAdquisicionActual", Input.CostoAdquisicion);
        AgregarParametro(command, "@CategoriaId", Input.CategoriaId);
        AgregarParametro(command, "@MarcaId", Input.MarcaId);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void AgregarParametro(
    System.Data.IDbCommand command,
    string nombre,
    object valor)
    {
        var parametro = command.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        command.Parameters.Add(parametro);
    }

    private void RegistrarHistoricoInicial(
        int productoId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText = @"
        INSERT INTO HistoricoCostoProducto
        (
            ProductoId,
            CostoAdquisicion,
            Motivo
        )
        VALUES
        (
            @ProductoId,
            @CostoAdquisicion,
            @Motivo
        )";

        AgregarParametro(command, "@ProductoId", productoId);
        AgregarParametro(command, "@CostoAdquisicion", Input.CostoAdquisicion);
        AgregarParametro(command, "@Motivo", "Registro inicial");

        command.ExecuteNonQuery();
    }

    private void RegistrarProductoConHistorico()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            int productoId = RegistrarProducto(connection, transaction);

            RegistrarHistoricoInicial(
                productoId,
                connection,
                transaction
            );

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

}

public class ProductoInput
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    public string? DescripcionEspecifica { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int Stock { get; set; }

    [Range(typeof(decimal), "0", "99999999.99",
        ErrorMessage = "El precio de venta no puede ser negativo.")]
    public decimal PrecioVenta { get; set; }

    [Range(typeof(decimal), "0", "99999999.99",
        ErrorMessage = "El costo de adquisición no puede ser negativo.")]
    public decimal CostoAdquisicion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una categoría.")]
    public int CategoriaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una marca.")]
    public int MarcaId { get; set; }
}

public record CategoriaOption(int CategoriaId, string Nombre);

public record MarcaOption(int MarcaId, string Nombre);