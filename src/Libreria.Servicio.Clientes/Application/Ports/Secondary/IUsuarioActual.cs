namespace Libreria.Servicio.Clientes.Application.Ports.Secondary;

/// <summary>
/// Puerto secundario: quién está operando. Lo usa la bitácora para llenar
/// UsuarioCreacionId y UsuarioModificacionId con el `sub` del JWT (contrato 6.4).
/// </summary>
public interface IUsuarioActual
{
    Guid? UsuarioId { get; }
    string? NombreUsuario { get; }
    string? Iniciales { get; }
}
