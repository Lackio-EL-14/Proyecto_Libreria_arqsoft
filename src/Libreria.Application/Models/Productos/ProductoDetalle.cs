using Libreria.Application.Domain;
using Libreria.Application.Models;
using Libreria.Application.Ports.Secondary;
using Libreria.Application.Factories;
namespace Libreria.Application.Models;

public record ProductoDetalle(
    int ProductoId,
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
