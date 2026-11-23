using System.ComponentModel.DataAnnotations;

namespace Libreria.Client.DTOs.Clientes;

// Cuerpo del alta y la edición de clientes. Los mensajes son los mismos de ClienteValidator del
// MS Clientes, que vuelve a validar todo (pruebas del 22/11, A4).
public class ClienteInput
{
    [Display(Name = "CI/NIT")]
    [Required(ErrorMessage = "El CI/NIT es obligatorio.")]
    [StringLength(30, ErrorMessage = "El CI/NIT no puede superar los 30 caracteres.")]
    public string CiNit { get; set; } = string.Empty;

    [Display(Name = "Razón social")]
    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [StringLength(200, ErrorMessage = "La razón social no puede superar los 200 caracteres.")]
    public string RazonSocial { get; set; } = string.Empty;

    [Display(Name = "Correo electrónico")]
    [StringLength(254, ErrorMessage = "El correo no puede superar los 254 caracteres.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico válido.")]
    public string? Correo { get; set; }
}

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
