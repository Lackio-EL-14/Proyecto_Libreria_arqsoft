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
