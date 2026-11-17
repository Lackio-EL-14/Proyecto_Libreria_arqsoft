using Libreria.Client.Binding;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Client.DTOs.Productos;

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

    public Guid CategoriaPublicId { get; set; }

    public Guid MarcaPublicId { get; set; }
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
