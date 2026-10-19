namespace Libreria.Application.Models;

// Datos internos para comprobar la baja; no se envían a la UI.
public record UsuarioBajaDatos(
    int UsuarioId, Guid PublicId, string Estado, string Rol, bool RolActivo);
