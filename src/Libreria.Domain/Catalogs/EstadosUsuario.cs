namespace Libreria.Domain;

public static class EstadosUsuario
{
    public const string Pendiente = "Pendiente";
    public const string Activo = "Activo";
    public const string Inactivo = "Inactivo";

    public static IReadOnlyList<string> Todos { get; } =
    [
        Pendiente,
        Activo,
        Inactivo
    ];

    public static bool EsValido(string? estado)
    {
        return estado is not null
            && Todos.Contains(estado, StringComparer.Ordinal);
    }
}