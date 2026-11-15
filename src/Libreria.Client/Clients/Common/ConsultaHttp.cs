namespace Libreria.Client.Clients.Common;

/// <summary>Arma la cadena de consulta (<c>?a=1&amp;b=2</c>) omitiendo los valores vacíos.</summary>
public static class ConsultaHttp
{
    public static string Armar(params (string Clave, string? Valor)[] parametros)
    {
        var partes = parametros
            .Where(p => !string.IsNullOrWhiteSpace(p.Valor))
            .Select(p => $"{p.Clave}={Uri.EscapeDataString(p.Valor!.Trim())}")
            .ToList();

        return partes.Count == 0 ? string.Empty : "?" + string.Join('&', partes);
    }

    public static string Booleano(bool valor) => valor ? "true" : "false";
}
