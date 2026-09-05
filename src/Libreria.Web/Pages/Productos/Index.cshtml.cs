using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Web.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IndexModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<ProductoListItem> Productos { get; private set; } = new();
    public List<CategoriaFiltroItem> Categorias { get; private set; } = new();
    public List<MarcaFiltroItem> Marcas { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Busqueda { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CategoriaId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MarcaId { get; set; }

    public void OnGet()
    {
        CargarCategorias();
        CargarMarcas();

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
            SELECT 
                p.ProductoId,
                p.Nombre,
                p.Stock,
                p.PrecioVenta,
                p.CostoAdquisicionActual,
                c.Nombre AS Categoria,
                m.Nombre AS Marca
            FROM Producto p
            INNER JOIN Categoria c ON p.CategoriaId = c.CategoriaId
            INNER JOIN Marca m ON p.MarcaId = m.MarcaId
            WHERE p.Estado = 1
              AND (
                    @Busqueda IS NULL
                    OR @Busqueda = ''
                    OR p.Nombre LIKE '%' + @Busqueda + '%'
                    OR p.DescripcionEspecifica LIKE '%' + @Busqueda + '%'
                  )
            AND (@CategoriaId IS NULL OR p.CategoriaId = @CategoriaId)
            AND (@MarcaId IS NULL OR p.MarcaId = @MarcaId)           
            ORDER BY p.Nombre";

        var parametroBusqueda = command.CreateParameter();
        parametroBusqueda.ParameterName = "@Busqueda";
        parametroBusqueda.Value = string.IsNullOrWhiteSpace(Busqueda)
            ? DBNull.Value
            : Busqueda.Trim();

        command.Parameters.Add(parametroBusqueda);

        var parametroCategoria = command.CreateParameter();
        parametroCategoria.ParameterName = "@CategoriaId";
        parametroCategoria.Value = CategoriaId.HasValue
            ? CategoriaId.Value
            : DBNull.Value;

        command.Parameters.Add(parametroCategoria);

        var parametroMarca = command.CreateParameter();
        parametroMarca.ParameterName = "@MarcaId";
        parametroMarca.Value = MarcaId.HasValue
            ? MarcaId.Value
            : DBNull.Value;

        command.Parameters.Add(parametroMarca);


        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            Productos.Add(new ProductoListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4),
                reader.GetString(5),
                reader.GetString(6)
            ));
        }

    }

    private void CargarCategorias()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT CategoriaId, Nombre
        FROM Categoria
        ORDER BY Nombre";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            Categorias.Add(new CategoriaFiltroItem(
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
        ORDER BY Nombre";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            Marcas.Add(new MarcaFiltroItem(
                reader.GetInt32(0),
                reader.GetString(1)
            ));
        }
    }


}


public record ProductoListItem(
    int ProductoId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    string Categoria,
    string Marca
);

public record CategoriaFiltroItem(
    int CategoriaId,
    string Nombre
);

public record MarcaFiltroItem(
    int MarcaId,
    string Nombre
);