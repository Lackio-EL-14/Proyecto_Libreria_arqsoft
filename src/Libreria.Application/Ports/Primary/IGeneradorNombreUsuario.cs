namespace Libreria.Application.Ports.Primary;

public interface IGeneradorNombreUsuario
{
    Task<string> GenerarAsync(string nombre, string apellido);
}