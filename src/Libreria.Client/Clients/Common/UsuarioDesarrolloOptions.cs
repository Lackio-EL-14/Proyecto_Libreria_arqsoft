namespace Libreria.Client.Clients.Common;

public sealed class UsuarioDesarrolloOptions
{
    public const string SectionName = "UsuarioDesarrollo";

    public string Id { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
}
