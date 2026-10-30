using System;
namespace Libreria.Client.Pages.Productos;

public record ProductoListItem(Guid PublicId, string Codigo, string Nombre, string Categoria, string Marca, decimal CostoAdquisicionActual, decimal PrecioVenta, int Stock, bool EsPerecedero, bool Activo);
public record CategoriaOption(int CategoriaId, string Nombre);
public record MarcaOption(int MarcaId, string Nombre);

public record ProductoDetalle(Guid PublicId, string Codigo, string Nombre, string? Descripcion, string? DescripcionEspecifica, int CategoriaId, string Categoria, int MarcaId, string Marca, decimal PrecioCompra, decimal CostoAdquisicionActual, decimal PrecioVenta, int StockMinimo, int Stock, bool Activo, bool EsPerecedero, int UsuarioCreacionId, DateTime FechaCreacion, int? UsuarioModificacionId, DateTime? FechaModificacion, DateTime? FechaVencimiento);
