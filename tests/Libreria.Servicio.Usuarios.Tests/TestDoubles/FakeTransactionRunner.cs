using Libreria.Servicio.Usuarios.Application.Abstractions;

namespace Libreria.Servicio.Usuarios.Tests.TestDoubles;

public sealed class FakeTransactionRunner : ITransactionRunner
{
    public bool SerializableExecuted { get; private set; }

    public Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation) =>
        operation();

    public Task<T> ExecuteSerializableAsync<T>(
        Func<Task<T>> operation)
    {
        SerializableExecuted = true;
        return operation();
    }
}