using System.ComponentModel.DataAnnotations;

namespace Libreria.Servicio.Ventas.Controllers.Dtos;

public record AnularVentaRequest(
    [Required(ErrorMessage = "Debe ingresar el motivo de la anulación.")]
    [StringLength(250, MinimumLength = 10, ErrorMessage = "El motivo de la anulación debe tener entre 10 y 250 caracteres.")]
    string Motivo);
