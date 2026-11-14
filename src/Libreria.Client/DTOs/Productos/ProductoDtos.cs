using Libreria.Client.Binding;
using Microsoft.AspNetCore.Mvc;

namespace Libreria.Client.DTOs.Ventas;

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

public record CategoriaOptionDto(int CategoriaId, string Nombre);

public record MarcaOptionDto(int MarcaId, string Nombre);

public record ProductoListItemDto(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    string Categoria,
    string Marca,
    bool EsPerecedero);

public record ProductoListadoDto(
    IReadOnlyList<ProductoListItemDto> Productos,
    IReadOnlyList<CategoriaOptionDto> Categorias,
    IReadOnlyList<MarcaOptionDto> Marcas);

public record ProductoFormularioDto(
    IReadOnlyList<CategoriaOptionDto> Categorias,
    IReadOnlyList<MarcaOptionDto> Marcas);

public record ProductoDetalleDto(
    string Nombre,
    string? DescripcionEspecifica,
    bool EsPerecedero,
    DateTime? FechaVencimiento,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    int CategoriaId,
    string Categoria,
    int MarcaId,
    string Marca,
    int? UsuarioCreacionId = null,
    DateTime? FechaCreacion = null,
    int? UsuarioModificacionId = null,
    DateTime? FechaModificacion = null);

public record ProductoEdicionDto(
    ProductoInput Input,
    IReadOnlyList<CategoriaOptionDto> Categorias,
    IReadOnlyList<MarcaOptionDto> Marcas);
