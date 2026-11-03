
namespace Libreria.Client.DTOs.Ventas;

public record ClienteVentaBusquedaDto(
    string CiNit,
    string RazonSocial
);

public record ProductoVentaBusquedaDto(
    Guid PublicId,
    string Nombre,
    int Stock,
    decimal PrecioVenta
);
