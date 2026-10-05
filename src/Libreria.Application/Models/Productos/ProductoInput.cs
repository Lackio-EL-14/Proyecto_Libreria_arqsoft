using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Application.Models;

public class ProductoInput
{
    public string Nombre { get; set; } = string.Empty;

    public string? DescripcionEspecifica { get; set; }
    public bool EsPerecedero { get; set; }

    public DateTime? FechaVencimiento { get; set; }

    public int Stock { get; set; }

    [ModelBinder(BinderType = typeof(DecimalInvariantModelBinder))]
    public decimal PrecioVenta { get; set; }

    [ModelBinder(BinderType = typeof(DecimalInvariantModelBinder))]
    public decimal CostoAdquisicion { get; set; }

    public int CategoriaId { get; set; }

    public int MarcaId { get; set; }
}
