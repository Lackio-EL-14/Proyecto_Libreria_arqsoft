using System.Transactions;
using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Infrastructure.Transactions;

public sealed class TransactionRunner : ITransactionRunner
{
    public Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation) =>
        ExecuteAsync(
            operation,
            IsolationLevel.ReadCommitted);

    public Task<T> ExecuteSerializableAsync<T>(
        Func<Task<T>> operation) =>
        ExecuteAsync(
            operation,
            IsolationLevel.Serializable);

    private static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        IsolationLevel isolationLevel)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = isolationLevel
            },
            TransactionScopeAsyncFlowOption.Enabled);

        var result = await operation();

        scope.Complete();

        return result;
    }
}