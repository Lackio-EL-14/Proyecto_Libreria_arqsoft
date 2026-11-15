using Libreria.Servicio.Usuarios.Application.Facades;
using Libreria.Servicio.Usuarios.Application.Services;
using Libreria.Servicio.Usuarios.Application.UseCases.Users;
using Libreria.Servicio.Usuarios.Domain.Constants;
using Libreria.Servicio.Usuarios.Domain.Entities;
using Libreria.Servicio.Usuarios.Tests.TestDoubles;

namespace Libreria.Servicio.Usuarios.Tests;

public sealed class ResendActivationUseCaseTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 3, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PendingUserAfterOneMinuteReceivesRotatedActivation()
    {
        var publicId = Guid.NewGuid();
        var repository = CreateRepository(publicId);
        var email = new FakeEmailSender();
        var useCase = CreateUseCase(repository, email);

        var sent = await useCase.ExecuteAsync(publicId);

        Assert.True(sent);
        Assert.NotNull(repository.UpdatedActivationTokenHash);
        Assert.NotEqual(
            repository.UserByPublicId!.ActivationTokenHash,
            repository.UpdatedActivationTokenHash);
        Assert.Equal(Now.UtcDateTime.AddHours(24),
            repository.UpdatedActivationExpiresAt);
        Assert.Equal(1, repository.ActivationModifiedByUserId);
        Assert.Equal("pendiente@example.com", email.Recipient);
        Assert.Contains("/Usuarios/Activar", email.HtmlBody);
        Assert.Contains("jpendiente", email.HtmlBody);
    }

    [Fact]
    public async Task ResendBeforeOneMinuteIsRejectedWithoutEmail()
    {
        var publicId = Guid.NewGuid();
        var repository = CreateRepository(publicId);
        repository.UserByPublicId!.ActivationTokenExpiresAt =
            Now.UtcDateTime.AddHours(24).AddSeconds(-30);
        var email = new FakeEmailSender();
        var useCase = CreateUseCase(repository, email);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => useCase.ExecuteAsync(publicId));

        Assert.Contains("un minuto", exception.Message);
        Assert.Null(email.Recipient);
        Assert.Null(repository.UpdatedActivationTokenHash);
    }

    [Fact]
    public async Task NonAdministratorCannotResendActivation()
    {
        var publicId = Guid.NewGuid();
        var repository = CreateRepository(publicId);
        repository.IsActiveAdministrator = false;
        var email = new FakeEmailSender();
        var useCase = CreateUseCase(repository, email);

        Assert.False(await useCase.ExecuteAsync(publicId));
        Assert.Null(email.Recipient);
    }

    private static FakeUserRepository CreateRepository(Guid publicId) =>
        new()
        {
            IsActiveAdministrator = true,
            UserByPublicId = new User
            {
                Id = 9,
                PublicId = publicId,
                FirstName = "Juan",
                Email = "pendiente@example.com",
                Username = "jpendiente",
                Status = UserStatuses.Pending,
                ActivationTokenHash = "hash-anterior",
                ActivationTokenExpiresAt = Now.UtcDateTime.AddHours(23),
                ActivationTokenUsedAt = null
            }
        };

    private static ResendActivationUseCase CreateUseCase(
        FakeUserRepository repository,
        FakeEmailSender email)
    {
        var clock = new FixedTimeProvider(Now);
        var facade = new UsuarioFacade(
            new UsernameGenerator(repository),
            email,
            new FakeApplicationConfiguration(),
            clock);

        return new ResendActivationUseCase(
            repository,
            new FakeCurrentUser { UserId = 1, Username = "admin" },
            facade,
            new FakeTransactionRunner(),
            clock);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
