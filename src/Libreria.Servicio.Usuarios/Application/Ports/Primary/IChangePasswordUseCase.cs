using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Domain;

namespace Libreria.Servicio.Usuarios.Application.Ports.Primary;

public interface IChangePasswordUseCase
{
    /// <summary>
    /// Cambia la contraseña del usuario actual en Keycloak. Devuelve <c>null</c> si no hay
    /// un usuario actual identificado (el controlador responde 403).
    /// </summary>
    Task<Result?> ExecuteAsync(ChangePasswordRequest request);
}
