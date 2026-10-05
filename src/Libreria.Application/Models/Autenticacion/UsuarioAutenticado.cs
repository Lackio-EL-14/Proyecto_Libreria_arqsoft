namespace Libreria.Application.Models;

public record UsuarioAutenticado(
    int UsuarioId,
    Guid PublicId,
    string NombreUsuario,
    string NombreCompleto,
    string Rol);
