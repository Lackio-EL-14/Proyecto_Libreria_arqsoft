namespace Libreria.Application.Models;

public record ClienteRegistroResult(
    bool Exitoso,
    ClienteVentaItem? Cliente,
    IReadOnlyDictionary<string, string> Errores)
{
    public static ClienteRegistroResult Correcto(ClienteVentaItem cliente) =>
        new(true, cliente, new Dictionary<string, string>());

    public static ClienteRegistroResult Invalido(IReadOnlyDictionary<string, string> errores) =>
        new(false, null, errores);
}
