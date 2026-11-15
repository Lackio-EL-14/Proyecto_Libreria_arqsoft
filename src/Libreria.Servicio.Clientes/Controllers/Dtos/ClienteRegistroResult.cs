using Libreria.Servicio.Clientes.Application.Models;

namespace Libreria.Servicio.Clientes.Controllers.Dtos;

/// <summary>
/// Respuesta de <c>POST api/clientes</c>. Conserva la forma que la alta rápida de la venta
/// ya consume (contrato estable, sección 6.5); <see cref="ClienteVentaItem"/> añade el PublicId.
/// </summary>
public sealed record ClienteRegistroResult(
    bool Exitoso,
    ClienteVentaItem? Cliente,
    IReadOnlyDictionary<string, string> Errores)
{
    public static ClienteRegistroResult Correcto(ClienteDetalle cliente) =>
        new(true, new ClienteVentaItem(cliente.PublicId, cliente.CiNit, cliente.RazonSocial),
            new Dictionary<string, string>());
}
