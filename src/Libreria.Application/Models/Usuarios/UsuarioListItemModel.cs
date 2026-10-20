namespace Libreria.Application.Models;

public record UsuarioListItemModel(
    Guid PublicId,
    string NombreCompleto,
    string NombreUsuario,
    string Correo,
    string Rol,
    string Estado,
    DateTime FechaCreacion,
    string? CreadoPor,
    string? ModificadoPor = null,
    DateTime? FechaModificacion = null);
