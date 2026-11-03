
namespace Libreria.Client.DTOs.Ventas;

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
