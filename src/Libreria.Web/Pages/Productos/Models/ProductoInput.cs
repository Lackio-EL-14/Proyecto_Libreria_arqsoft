using Microsoft.AspNetCore.Mvc;

namespace Libreria.Web.Pages.Productos.Models;

public class ProductoInput
{
    public string Nombre { get; set; } = string.Empty;

    public string? DescripcionEspecifica { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int Stock { get; set; }

    [ModelBinder(BinderType = typeof(DecimalInvariantModelBinder))]
    public decimal PrecioVenta { get; set; }

    [ModelBinder(BinderType = typeof(DecimalInvariantModelBinder))]
    public decimal CostoAdquisicion { get; set; }

    public int CategoriaId { get; set; }

    public int MarcaId { get; set; }
}
