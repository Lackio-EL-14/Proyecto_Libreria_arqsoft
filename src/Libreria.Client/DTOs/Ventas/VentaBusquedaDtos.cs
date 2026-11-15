
namespace Libreria.Client.DTOs.Ventas;

public record ProductoVentaBusquedaDto(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta
);
