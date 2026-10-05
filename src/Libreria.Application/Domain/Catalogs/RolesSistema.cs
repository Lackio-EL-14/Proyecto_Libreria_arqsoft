namespace Libreria.Application.Domain;

public static class RolesSistema
{
    public const string Administrador = "Administrador";
    public const string Vendedor = "Vendedor";

    public static IReadOnlyList<string> Todos { get; } =
    [
        Administrador,
        Vendedor
    ];
}
