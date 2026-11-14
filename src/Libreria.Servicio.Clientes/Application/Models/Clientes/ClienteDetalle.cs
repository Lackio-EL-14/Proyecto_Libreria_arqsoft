using Libreria.Servicio.Clientes.Domain;

namespace Libreria.Servicio.Clientes.Application.Models;

/// <summary>Cliente completo para el listado y la edición, con su auditoría. Sin ids numéricos.</summary>
public record ClienteDetalle(
    Guid PublicId,
    string CiNit,
    string RazonSocial,
    string? Correo,
    bool Estado,
    DateTime FechaCreacion,
    Guid? UsuarioCreacionId,
    DateTime? FechaModificacion,
    Guid? UsuarioModificacionId)
{
    public static ClienteDetalle Desde(Cliente cliente) => new(
        cliente.PublicId,
        cliente.CiNit,
        cliente.RazonSocial,
        cliente.Correo,
        cliente.Estado,
        cliente.FechaCreacion,
        cliente.UsuarioCreacionId,
        cliente.FechaModificacion,
        cliente.UsuarioModificacionId);
}
