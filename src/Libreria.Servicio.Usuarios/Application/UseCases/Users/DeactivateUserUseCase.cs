using Libreria.Servicio.Usuarios.Application.Ports.Primary;
using Libreria.Servicio.Usuarios.Application.Ports.Secondary;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class DeactivateUserUseCase : IDeactivateUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IProveedorIdentidad _proveedorIdentidad;

    public DeactivateUserUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        ITransactionRunner transactionRunner,
        IProveedorIdentidad proveedorIdentidad)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _transactionRunner = transactionRunner;
        _proveedorIdentidad = proveedorIdentidad;
    }

    /// <summary>Keycloak falló dentro de la transacción: se lanza para revertir la baja en la BD.</summary>
    private sealed class ProveedorIdentidadNoDisponibleException(Exception inner)
        : Exception("El proveedor de identidad no respondió.", inner);

    public async Task<DeactivateUserResult> ExecuteAsync(
        Guid publicId)
    {
        var actorId = _currentUser.UserId;

        if (actorId is null || actorId == Guid.Empty)
        {
            return DeactivateUserResult.ActorNotAuthorized;
        }

        if (publicId == Guid.Empty)
        {
            return DeactivateUserResult.NotFound;
        }

        try
        {
            return await DarDeBajaAsync(publicId, actorId.Value);
        }
        catch (ProveedorIdentidadNoDisponibleException)
        {
            return DeactivateUserResult.IdentityProviderUnavailable;
        }
    }

    private Task<DeactivateUserResult> DarDeBajaAsync(Guid publicId, Guid actorId) =>
        _transactionRunner.ExecuteSerializableAsync(
            async () =>
            {
                var users =
                    await _userRepository.GetUsersForDeactivationAsync();

                // El usuario actual llega como sub de Keycloak (US-125).
                var actor =
                    users.SingleOrDefault(
                        user => user.KeycloakId == actorId);

                if (actor is null ||
                    !IsActiveAdministrator(actor))
                {
                    return DeactivateUserResult.ActorNotAuthorized;
                }

                var user =
                    users.SingleOrDefault(
                        candidate =>
                            candidate.PublicId == publicId);

                if (user is null)
                {
                    return DeactivateUserResult.NotFound;
                }

                if (user.Status == UserStatuses.Inactive)
                {
                    return DeactivateUserResult.AlreadyInactive;
                }

                if (IsActiveAdministrator(user) &&
                    users.Count(IsActiveAdministrator) <= 1)
                {
                    return DeactivateUserResult.LastActiveAdministrator;
                }

                if (user.KeycloakId == actorId)
                {
                    return DeactivateUserResult.SelfDeactivation;
                }

                var deactivated =
                    await _userRepository.DeactivateAsync(
                        user.UserId,
                        actorId);

                if (!deactivated)
                {
                    throw new InvalidOperationException(
                        "No se pudo actualizar el estado del usuario.");
                }

                await DeshabilitarEnKeycloakAsync(user.KeycloakId);

                return DeactivateUserResult.Success;
            });

    /// <summary>
    /// US-128: el usuario dado de baja no puede volver a iniciar sesión y pierde sus sesiones
    /// abiertas. Si aún no tiene cuenta en Keycloak (alta anterior a US-130), basta la BD.
    /// </summary>
    private async Task DeshabilitarEnKeycloakAsync(Guid? keycloakId)
    {
        if (keycloakId is not Guid id)
        {
            return;
        }

        try
        {
            var resultado = await _proveedorIdentidad.DeshabilitarAsync(id);
            if (resultado.IsError && resultado.Tipo != Domain.TipoError.NoEncontrado)
            {
                throw new InvalidOperationException(string.Join(" ", resultado.Errors()));
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            throw new ProveedorIdentidadNoDisponibleException(exception);
        }
    }

    private static bool IsActiveAdministrator(
        UserDeactivationData user) =>
        user.Status == UserStatuses.Active &&
        user.RoleActive &&
        user.Role == SystemRoles.Administrator;
}