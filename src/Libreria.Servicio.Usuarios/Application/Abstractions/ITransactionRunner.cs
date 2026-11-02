namespace Libreria.Servicio.Usuarios.Application.Abstractions;

public interface ITransactionRunner
{
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation);
}