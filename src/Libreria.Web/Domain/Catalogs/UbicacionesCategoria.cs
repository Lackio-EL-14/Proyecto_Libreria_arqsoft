namespace Libreria.Web.Domain.Catalogs;

public static class UbicacionesCategoria
{
    public const string EstantePrincipal = "Estante principal";
    public const string Deposito = "Depósito";
    public const string Vitrina = "Vitrina";
    public const string Bodega = "Bodega";
    public const string SinAsignar = "Sin asignar";

    public static IReadOnlyList<string> Todas { get; } =
    [
        EstantePrincipal,
        Deposito,
        Vitrina,
        Bodega,
        SinAsignar
    ];

    public static bool EsValida(string? ubicacion)
    {
        return ubicacion is not null
            && Todas.Contains(ubicacion, StringComparer.Ordinal);
    }
}
