using System.Transactions;
using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Infrastructure.Transactions;

public sealed class TransactionRunner : ITransactionRunner
{
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        using var scope = new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel =
                    IsolationLevel.ReadCommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);

        var result = await operation();

        scope.Complete();

        return result;
    }
}