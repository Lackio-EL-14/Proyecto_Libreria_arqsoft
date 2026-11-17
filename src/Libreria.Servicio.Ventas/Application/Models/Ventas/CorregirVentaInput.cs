namespace Libreria.Servicio.Ventas.Application.Models;

public sealed class CorregirVentaInput
{
    public string CiNitCliente { get; set; } = string.Empty;
    public string? Observacion { get; set; }
}
