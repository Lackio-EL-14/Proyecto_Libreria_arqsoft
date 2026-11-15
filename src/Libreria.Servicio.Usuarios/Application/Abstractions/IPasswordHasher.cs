namespace Libreria.Servicio.Usuarios.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);

    bool RequiresRehash(string hash);
}