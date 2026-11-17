using System.ComponentModel.DataAnnotations;

namespace Libreria.Servicio.Proveedores.Controllers.Dtos;

public sealed class CambioEstadoProveedor
{
    [Required(ErrorMessage = "Indique si el proveedor queda activo o inactivo.")]
    public bool? Activo { get; set; }
}
