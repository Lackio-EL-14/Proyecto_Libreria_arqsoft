using Libreria.Web.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Libreria.Web.Pages.Productos;

public class IndexModel : PageModel
{
    private readonly IDbConnectionFactory _connectionFactory;

    public IndexModel(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public List<ProductoListItem> Productos { get; private set; } = new();
    
    public void OnGet()
    {
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
            ORDER BY p.Nombre";

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