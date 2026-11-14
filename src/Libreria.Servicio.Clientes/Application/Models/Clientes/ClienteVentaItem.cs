namespace Libreria.Servicio.Clientes.Application.Models;

/// <summary>Cliente que la venta encuentra por CI/NIT (contrato 6.6: ClienteVentaItem + PublicId).</summary>
public record ClienteVentaItem(Guid PublicId, string CiNit, string RazonSocial);
