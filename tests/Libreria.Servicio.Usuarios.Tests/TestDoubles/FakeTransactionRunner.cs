using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeTransactionRunner
    : ITransactionRunner
{
    public Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation)
    {
        return operation();
    }
}