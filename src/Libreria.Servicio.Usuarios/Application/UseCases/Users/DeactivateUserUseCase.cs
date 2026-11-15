using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.DTOs.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class DeactivateUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly ITransactionRunner _transactionRunner;

    public DeactivateUserUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        ITransactionRunner transactionRunner)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _transactionRunner = transactionRunner;
    }

    public async Task<DeactivateUserResult> ExecuteAsync(
        Guid publicId)
    {
        var actorId = _currentUser.UserId;

        if (actorId is not > 0)
        {
            return DeactivateUserResult.ActorNotAuthorized;
        }

        if (publicId == Guid.Empty)
        {
            return DeactivateUserResult.NotFound;
        }

        return await _transactionRunner.ExecuteSerializableAsync(
            async () =>
            {
                var users =
                    await _userRepository.GetUsersForDeactivationAsync();

                var actor =
                    users.SingleOrDefault(
                        user => user.UserId == actorId.Value);

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

                if (user.UserId == actorId.Value)
                {
                    return DeactivateUserResult.SelfDeactivation;
                }

                var deactivated =
                    await _userRepository.DeactivateAsync(
                        user.UserId,
                        actorId.Value);

                if (!deactivated)
                {
                    throw new InvalidOperationException(
                        "No se pudo actualizar el estado del usuario.");
                }

                return DeactivateUserResult.Success;
            });
    }

    private static bool IsActiveAdministrator(
        UserDeactivationData user) =>
        user.Status == UserStatuses.Active &&
        user.RoleActive &&
        user.Role == SystemRoles.Administrator;
}