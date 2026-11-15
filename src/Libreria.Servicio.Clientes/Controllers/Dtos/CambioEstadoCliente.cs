using System.ComponentModel.DataAnnotations;

namespace Libreria.Servicio.Clientes.Controllers.Dtos;

/// <summary>
/// Cuerpo de <c>PATCH api/clientes/{publicId}/estado</c>: <c>false</c> da de baja (delete lógico),
/// <c>true</c> reactiva. Es obligatorio para que un cuerpo vacío no dé de baja por omisión.
/// </summary>
public sealed class CambioEstadoCliente
{
    [Required(ErrorMessage = "Indique si el cliente queda activo o inactivo.")]
    public bool? Activo { get; set; }
}
