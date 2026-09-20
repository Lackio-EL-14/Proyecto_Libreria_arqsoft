namespace Libreria.Web.Domain.Catalogs;

public static class PaisesMarca
{
    public const string NoEspecificado = "No especificado";

    public static IReadOnlyList<string> Todos { get; } = new[]
    {
        NoEspecificado,
        "Alemania",
        "Argentina",
        "Bolivia",
        "Brasil",
        "Canadá",
        "Chile",
        "China",
        "Colombia",
        "Corea del Sur",
        "Ecuador",
        "España",
        "Estados Unidos",
        "Francia",
        "India",
        "Italia",
        "Japón",
        "México",
        "Paraguay",
        "Perú",
        "Reino Unido",
        "Uruguay",
        "Venezuela"
    };

    public static bool EsValido(string? pais)
    {
        return pais is not null &&
               Todos.Contains(
                   pais,
                   StringComparer.OrdinalIgnoreCase);
    }

    public static string Normalizar(string? pais)
    {
        var valor = string.Join(
            ' ',
            (pais ?? string.Empty).Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries));

        return Todos.FirstOrDefault(
                   p => string.Equals(
                       p,
                       valor,
                       StringComparison.OrdinalIgnoreCase))
               ?? valor;
    }
}