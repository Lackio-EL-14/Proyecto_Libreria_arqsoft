namespace Libreria.Application.Models;

public class RegistrarVentaInput
{
    // Se conserva al reintentar la misma operación y se utiliza como PublicId de la venta.
    public Guid SolicitudId { get; set; }

    public string CiNitCliente { get; set; } = string.Empty;

    public IReadOnlyList<DetalleVentaInput> Detalles { get; set; }
        = Array.Empty<DetalleVentaInput>();
}
