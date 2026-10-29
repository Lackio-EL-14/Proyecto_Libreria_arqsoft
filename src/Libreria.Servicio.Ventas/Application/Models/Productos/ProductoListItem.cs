using Libreria.Domain;
using Libreria.Servicio.Ventas.Application.Models;
using Libreria.Servicio.Ventas.Application.Ports.Secondary;
using Libreria.Servicio.Ventas.Application.Factories;
namespace Libreria.Servicio.Ventas.Application.Models;

public record ProductoListItem(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta,
    decimal CostoAdquisicionActual,
    string Categoria,
    string Marca,
    bool EsPerecedero
);