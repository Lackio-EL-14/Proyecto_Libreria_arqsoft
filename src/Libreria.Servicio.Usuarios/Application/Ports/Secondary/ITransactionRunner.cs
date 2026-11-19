namespace Libreria.Servicio.Usuarios.Application.Ports.Secondary;

public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation);

    Task<T> ExecuteSerializableAsync<T>(
        Func<Task<T>> operation);
}