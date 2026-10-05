namespace Libreria.Application.Ports.Secondary;

public interface IContrasenaHasher
{
    string Hashear(string contrasena);
    bool Verificar(string hash, string contrasena);
}
