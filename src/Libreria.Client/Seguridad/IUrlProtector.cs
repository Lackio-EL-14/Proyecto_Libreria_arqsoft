namespace Libreria.Client.Seguridad;

public interface IUrlProtector
{
    string Cifrar(Guid publicId);
    Guid? Descifrar(string textoCifrado);
}
