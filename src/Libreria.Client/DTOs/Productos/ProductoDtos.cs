using System.ComponentModel.DataAnnotations;
using Libreria.Client.Binding;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Client.DTOs.Productos;

// Formulario de alta y edición. Los números y GUID son anulables para que un campo vacío
// muestre su mensaje propio y no «The value '' is invalid.» (pruebas del 22/11, C3).
// Los textos coinciden con ProductoValidator del MS Productos, que vuelve a validar todo.
public class ProductoInput
{
    [Display(Name = "Nombre")]
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    public string Nombre { get; set; } = string.Empty;

    [Display(Name = "Descripción específica")]
    public string? DescripcionEspecifica { get; set; }

    [Display(Name = "Es un producto perecedero")]
    public bool EsPerecedero { get; set; }

    [Display(Name = "Fecha de vencimiento")]
    public DateTime? FechaVencimiento { get; set; }

    [Display(Name = "Stock (unidades)")]
    [Required(ErrorMessage = "Ingrese el stock del producto.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
    public int? Stock { get; set; }

    [Display(Name = "Precio de venta (Bs.)")]
    [Required(ErrorMessage = "Ingrese el precio de venta.")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El precio de venta no puede ser negativo.")]
    [ModelBinder(BinderType = typeof(DecimalInvariantModelBinder))]
    public decimal? PrecioVenta { get; set; }

    [Display(Name = "Costo de adquisición (Bs.)")]
    [Required(ErrorMessage = "Ingrese el costo de adquisición.")]
    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "El costo de adquisición no puede ser negativo.")]
    [ModelBinder(BinderType = typeof(DecimalInvariantModelBinder))]
    public decimal? CostoAdquisicion { get; set; }

    [Display(Name = "Categoría")]
    [Required(ErrorMessage = "Seleccione una categoría.")]
    public Guid? CategoriaPublicId { get; set; }

    [Display(Name = "Marca")]
    [Required(ErrorMessage = "Seleccione una marca.")]
    public Guid? MarcaPublicId { get; set; }
}

// Cuerpo de POST/PUT api/productos: el mismo JSON de siempre (contrato sin cambios).
// Solo se arma después de validar el formulario, así que los campos obligatorios ya tienen valor.
public sealed record ProductoCuerpoDto(
    string Nombre,
    string? DescripcionEspecifica,
    bool EsPerecedero,
    DateTime? FechaVencimiento,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicion,
    Guid CategoriaPublicId,
    Guid MarcaPublicId)
{
    public static ProductoCuerpoDto Desde(ProductoInput input) => new(
        input.Nombre,
        input.DescripcionEspecifica,
        input.EsPerecedero,
        input.FechaVencimiento,
        input.Stock ?? 0,
        input.PrecioVenta ?? 0,
        input.CostoAdquisicion ?? 0,
        input.CategoriaPublicId ?? Guid.Empty,
        input.MarcaPublicId ?? Guid.Empty);
}

public record CategoriaOptionDto(
    Guid PublicId,
    string Nombre);

public record MarcaOptionDto(
    Guid PublicId,
    string Nombre);

public record ProductoListItemDto(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    Guid CategoriaPublicId,
    string Categoria,
    Guid MarcaPublicId,
    string Marca,
    bool EsPerecedero,
    bool Estado);

public record ProductoListadoDto(
    IReadOnlyList<ProductoListItemDto> Productos,
    IReadOnlyList<CategoriaOptionDto> Categorias,
    IReadOnlyList<MarcaOptionDto> Marcas);

public record ProductoFormularioDto(
    IReadOnlyList<CategoriaOptionDto> Categorias,
    IReadOnlyList<MarcaOptionDto> Marcas);

public record ProductoDetalleDto(
    Guid PublicId,
    string Nombre,
    string? DescripcionEspecifica,
    bool EsPerecedero,
    DateTime? FechaVencimiento,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    Guid CategoriaPublicId,
    string Categoria,
    Guid MarcaPublicId,
    string Marca,
    Guid? UsuarioCreacionId,
    DateTime FechaCreacion,
    Guid? UsuarioModificacionId,
    DateTime? FechaModificacion);

public record ProductoEdicionDto(
    ProductoInput Input,
    IReadOnlyList<CategoriaOptionDto> Categorias,
    IReadOnlyList<MarcaOptionDto> Marcas);
