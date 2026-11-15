namespace Libreria.Servicio.Usuarios.Domain.Constants;

public static class UserStatuses
{
    public const string Pending = "Pendiente";
    public const string Active = "Activo";
    public const string Inactive = "Inactivo";

    public static IReadOnlyList<string> All { get; } =
    [
        Pending,
        Active,
        Inactive
    ];

    public static bool IsValid(string? status)
    {
        return status is not null
            && All.Contains(status, StringComparer.Ordinal);
    }
}