using System;
namespace Libreria.Application.Ports.Primary
{
    public interface IUrlProtector
    {
        string Cifrar(Guid publicId);
        Guid? Descifrar(string textoCifrado);
    }
}
