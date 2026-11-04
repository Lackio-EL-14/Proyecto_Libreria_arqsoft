using Libreria.Servicio.Usuarios.Application.Abstractions;
using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Domain.Constants;

namespace Libreria.Servicio.Usuarios.Application.UseCases.Users;

public sealed class ResendActivationUseCase
{
    private static readonly TimeSpan ResendInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;
    private readonly UsuarioFacade _usuarioFacade;
    private readonly ITransactionRunner _transactionRunner;
    private readonly TimeProvider _timeProvider;

    public ResendActivationUseCase(
        IUserRepository userRepository,
        ICurrentUser currentUser,
        UsuarioFacade usuarioFacade,
        ITransactionRunner transactionRunner,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _usuarioFacade = usuarioFacade;
        _transactionRunner = transactionRunner;
        _timeProvider = timeProvider;
    }

    public async Task<bool> ExecuteAsync(Guid publicId)
    {
        if (publicId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario es obligatorio.",
                nameof(publicId));
        }

        if (_currentUser.UserId is not int currentUserId ||
            !await _userRepository.IsActiveAdministratorAsync(currentUserId))
        {
            return false;
        }

        var user = await _userRepository.GetByPublicIdAsync(publicId)
            ?? throw new KeyNotFoundException(
                "No se encontró el usuario solicitado.");

        if (!string.Equals(
                user.Status,
                UserStatuses.Pending,
                StringComparison.OrdinalIgnoreCase) ||
            user.ActivationTokenUsedAt is not null)
        {
            throw new InvalidOperationException(
                "Solo se puede reenviar la activación de un usuario pendiente.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var lastSentAt = user.ActivationTokenExpiresAt?.Subtract(TokenLifetime);

        if (lastSentAt.HasValue && now - lastSentAt.Value < ResendInterval)
        {
            throw new InvalidOperationException(
                "Espere al menos un minuto antes de reenviar la activación.");
        }

        var activation = _usuarioFacade.PrepareActivation(user.Username);

        return await _transactionRunner.ExecuteAsync(
            async () =>
            {
                var updated = await _userRepository.UpdateActivationTokenAsync(
                    user.Id,
                    activation.TokenHash,
                    activation.ExpiresAt,
                    currentUserId,
                    now);

                if (!updated)
                {
                    throw new InvalidOperationException(
                        "El usuario dejó de estar pendiente antes del reenvío.");
                }

                await _usuarioFacade.SendActivationEmailAsync(
                    user.Email,
                    user.FirstName,
                    activation);

                return true;
            });
    }
}
