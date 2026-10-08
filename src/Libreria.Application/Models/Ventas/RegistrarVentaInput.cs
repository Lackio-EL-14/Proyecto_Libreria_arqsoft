namespace Libreria.Application.Models;

public class RegistrarVentaInput
{
    public string CiNitCliente { get; set; } = string.Empty;

    public IReadOnlyList<DetalleVentaInput> Detalles { get; set; }
        = Array.Empty<DetalleVentaInput>();
}