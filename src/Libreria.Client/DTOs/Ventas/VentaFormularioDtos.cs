
namespace Libreria.Client.DTOs.Ventas;

public class RegistrarVentaInput
{
    public Guid SolicitudId { get; set; }

    public string CiNitCliente { get; set; } = string.Empty;

    public IReadOnlyList<DetalleVentaInput> Detalles { get; set; }
        = Array.Empty<DetalleVentaInput>();
}

public class DetalleVentaInput
{
    public Guid ProductoPublicId { get; set; }

    public int Cantidad { get; set; }
}
