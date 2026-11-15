namespace Libreria.Servicio.Ventas.Application.Models;

public sealed record ClienteVentaSnapshot(
    Guid PublicId,
    string CiNit,
    string RazonSocial);
