namespace Libreria.Client.DTOs.Clientes;

// Alta rápida de cliente desde la venta (POST api/clientes).
public class ClienteInput
{
    public string CiNit { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Correo { get; set; }
}

public record ClienteRegistroResult(
    bool Exitoso,
    ClienteVentaBusquedaDto? Cliente,
    IReadOnlyDictionary<string, string> Errores)
{
    public static ClienteRegistroResult Invalido(
        IReadOnlyDictionary<string, string> errores) =>
        new(false, null, errores);
}

// Cliente que la venta busca por CI/NIT.
public record ClienteVentaBusquedaDto(
    string CiNit,
    string RazonSocial
);

// Cliente tal como lo devuelve el MS Clientes (GET api/clientes y api/clientes/{publicId}).
// La auditoría guarda el PublicId del usuario (sub del JWT).
public record ClienteDetalleDto(
    Guid PublicId,
    string CiNit,
    string RazonSocial,
    string? Correo,
    bool Estado,
    DateTime FechaCreacion,
    Guid? UsuarioCreacionId,
    DateTime? FechaModificacion,
    Guid? UsuarioModificacionId);

// Qué clientes lista la página: el MS distingue «sin enviar» (activos), false e «estado=» (todos).
public enum EstadoFiltroClientes
{
    Activos,
    Inactivos,
    Todos
}

// Columnas por las que el MS sabe ordenar (lista cerrada del contrato).
public enum OrdenClientes
{
    RazonSocial,
    CiNit,
    Correo,
    FechaCreacion
}

public sealed record FiltroClientes(
    string? Busqueda = null,
    EstadoFiltroClientes Estado = EstadoFiltroClientes.Activos,
    OrdenClientes Orden = OrdenClientes.RazonSocial,
    bool Descendente = false);

// Cuerpo de PATCH api/clientes/{publicId}/estado.
public sealed record CambioEstadoClienteDto(bool Activo);
